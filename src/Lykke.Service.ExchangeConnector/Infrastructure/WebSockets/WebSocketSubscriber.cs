using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Polly;

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
                if (_state == State.Starting || _state == State.Stopping || _state == State.Disposed || _state == State.Stopped)
                {
                    return;
                }

                StopImpl();
                StartImpl();
            }
        }

        private void RechargeHeartbeat()
        {
            _heartBeatMonitoringTimer.Change(_heartBeatPeriod, Timeout.InfiniteTimeSpan);
        }

        public void Subscribe(Func<string, Task> messageHandler)
        {
            ValidateInstance();

            Handler = messageHandler;
        }

        protected virtual async Task HandleResponse(string json, CancellationToken token)
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

        protected virtual Task Connect(CancellationToken token)
        {
            return Messenger.ConnectAsync(token);

        }

        protected async Task MessageLoopImpl()
        {
            try
            {
                await Connect(CancellationToken);

                RechargeHeartbeat();
                while (!CancellationToken.IsCancellationRequested)
                {
                    var response = await Messenger.GetResponseAsync(CancellationToken);
                    RechargeHeartbeat();
                    await HandleResponse(response, CancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore task canceled exception which is happening on stopping
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
        }
        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is AuthenticationException) && !CancellationToken.IsCancellationRequested)
                .WaitAndRetryForeverAsync(attempt => attempt % 60 == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout)); // After every 60 attempts wait 5min 

            await retryPolicy.ExecuteAsync(async () =>
            {
                await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                try
                {
                    await MessageLoopImpl();
                }
                catch (AuthenticationException ex)
                {
                    await Log.WriteErrorAsync(nameof(MessageLoopImpl), $"AuthenticationException. Will not try to reconnect. Check the API keys in the settings", ex);
                    _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    throw;
                }
                catch (Exception ex)
                {
                    _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    await Log.WriteErrorAsync(nameof(MessageLoopImpl), $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                    throw;
                }
            });
        }


        private void StartImpl()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask = Task.Run(MessageLoop, _cancellationTokenSource.Token);
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
            catch (AuthenticationException)
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
