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
        protected readonly WebSocketTextMessenger Messenger;
        protected Func<string, Task> Handler;
        protected CancellationToken CancellationToken;

        private Task _messageLoopTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod = TimeSpan.FromSeconds(30);
        private static readonly object _sync = new object();
        private bool _isStarted = false;
        private bool _isDisposed = false;

        public WebSocketSubscriber(string uri, ILog log)
        {
            Log = log.CreateComponentScope(GetType().Name);
            Messenger = new WebSocketTextMessenger(uri, Log);
            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private async void ForceStopMessenger(object state)
        {
            ValidateInstance();

            await Log.WriteWarningAsync(nameof(ForceStopMessenger), "Monitoring heartbeat", $"Heart stopped. Restarting {GetType().Name}");
            Stop();
            try
            {
                await _messageLoopTask;
            }
            catch (OperationCanceledException)
            {
            }
            Start();
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

            if (_isStarted)
                return;

            lock (_sync)
            {
                if (_isStarted)
                    return;

                _isStarted = true;
            }

            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask = Task.Run(async () => await MessageLoop());
        }

        public virtual void Stop()
        {
            ValidateInstance();

            if (!_isStarted)
                return;

            lock (_sync)
            {
                if (!_isStarted)
                    return;
                _isStarted = false;
            }

            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").Wait();
            _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _cancellationTokenSource?.Cancel();
            try
            {
                _messageLoopTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                Stop();
                Messenger?.Dispose();
                _messageLoopTask?.Dispose();
                _heartBeatMonitoringTimer?.Dispose();
                _isDisposed = true;
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

        private void ValidateInstance()
        {
            if (_isDisposed) { throw new InvalidOperationException("Calling disposed instance."); }
        }

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
    }
}
