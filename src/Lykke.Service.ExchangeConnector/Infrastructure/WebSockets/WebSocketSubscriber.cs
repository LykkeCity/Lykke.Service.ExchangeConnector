using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Polly;
using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Infrastructure.WebSockets
{
    class WebSocketSubscriber : IDisposable
    {
        protected readonly ILog Log;
        protected readonly IMessenger<object, string> Messenger;
        protected Func<string, Task> Handler;
        protected CancellationToken CancellationToken;

        private Task _messageLoopTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod;
        private static readonly object _sync = new object();
        private volatile State _state = State.Stopped;

        public WebSocketSubscriber(IMessenger<object, string> messenger, ILog log, TimeSpan? heartbeatPeriod = null)
        {
            Log = log.CreateComponentScope(GetType().Name);
            Messenger = messenger;
            _heartBeatPeriod = heartbeatPeriod ?? TimeSpan.FromSeconds(30);
            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private async void ForceStopMessenger(object state)
        {
            await Log.WriteWarningAsync(nameof(ForceStopMessenger), "Monitoring heartbeat", $"Heart stopped. Restarting {GetType().Name}");

            lock (_sync)
            {
                if (_state == State.Starting || _state == State.Stopping || _state == State.Disposed)
                {
                    return;
                }

                StopImpl();
                StartImpl();
            }
        }

        protected void RechargeHeartbeat()
        {
            _heartBeatMonitoringTimer.Change(_heartBeatPeriod, Timeout.InfiniteTimeSpan);
        }

        public WebSocketSubscriber Subscribe(Func<string, Task> messageHandler)
        {
            ValidateInstance();

            Handler = messageHandler;
            return this;
        }

        protected virtual async Task<Result> HandleResponse(string json, CancellationToken token)
        {
            if (Handler != null)
            {
                try
                {
                    await Handler(json);
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(WebSocketSubscriber), $"An exception occurred while handling message: '{json}'", ex);
                }
            }
            return Result.Ok;
        }

        public virtual void Start()
        {
            ValidateInstance();

            lock (_sync)
            {
                if (_state != State.Stopped)
                    return;

                _state = State.Starting;
                StartImpl();
                _state = State.Started;
            }
        }

        public virtual void Stop()
        {
            ValidateInstance();

            lock (_sync)
            {
                if (_state != State.Started)
                    return;

                _state = State.Stopping;
                StopImpl();
                _state = State.Stopped;
            }
        }

        protected virtual async Task<Result> Connect(CancellationToken token)
        {
            await Messenger.ConnectAsync(token);
            return Result.Ok;
        }

        protected virtual async Task<Result> MessageLoopImpl()
        {
            try
            {
                var result = await Connect(CancellationToken);
                if (result.IsFailure)
                {
                    return result;
                }

                RechargeHeartbeat();
                while (!CancellationToken.IsCancellationRequested)
                {
                    var response = await Messenger.GetResponseAsync(CancellationToken);
                    RechargeHeartbeat();
                    result = await HandleResponse(response, CancellationToken);
                    if (!result.Continue)
                    {
                        return result;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore task cancelled exception which is happenning on stopping
            }
            finally
            {
                try
                {
                    await Messenger.StopAsync(CancellationToken);
                }
                catch (Exception)
                {
                }
            }
            return Result.Ok;
        }

        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryForeverAsync(attempt => attempt % 60 == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout)); // After every 60 attempts wait 5min 

            await retryPolicy.ExecuteAsync(async (token) =>
            {
                await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                try
                {
                    var result = await MessageLoopImpl();
                    if (result.IsFailure && !result.Continue)
                    {
                        // Stop heartbeat timer
                        _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    }
                    else if (result.IsFailure && result.Continue)
                    {
                        throw new InvalidOperationException(result.Error); // retry
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(MessageLoopImpl), $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                    throw;
                }
            }, CancellationToken);
        }


        private void StartImpl()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask = Task.Run(async () => await MessageLoop());
        }

        private void StopImpl()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").Wait();
            _cancellationTokenSource?.Cancel();
            try
            {
                _messageLoopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            // Stop heartbeat timer can be recharged in the loop
            _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void ValidateInstance()
        {
            if (_state == State.Disposed)
            {
                throw new InvalidOperationException("Calling disposed instance.");
            }
        }

        #region "IDispose Implementation"
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_sync)
            {
                if (_state == State.Disposed)
                {
                    return;
                }

                StopImpl();
                _messageLoopTask?.Dispose();
                _heartBeatMonitoringTimer?.Dispose();

                _state = State.Disposed;
            }
        }
        #endregion

        protected class Result
        {
            public bool IsFailure { get; private set; }
            public string Error { get; private set; }
            public bool Continue { get; private set; }

            public Result(bool isFailure, bool _continue, string error = "")
            {
                this.IsFailure = isFailure;
                this.Continue = _continue;
                this.Error = error;
            }

            public static readonly Result Ok = new Result(false, true);
        }

        enum State
        {
            Stopped = 0,
            Starting,
            Started,
            Stopping,
            Disposed
        }
    }
}
