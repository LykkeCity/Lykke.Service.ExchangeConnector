using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingBot.Infrastructure.WebSockets
{
    internal class WebSocketSubscriber : IDisposable
    {
        protected readonly ILog Log;
        protected readonly IMessenger<object, string> Messenger;
        protected Func<string, Task> Handler;
        protected CancellationToken CancellationToken;

        private Task _messageLoopTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod;
        private readonly object _sync = new object();
        private State _state = State.Stopped;

        public WebSocketSubscriber(IMessenger<object, string> messenger, ILog log, TimeSpan? heartbeatPeriod = null)
        {
            Log = log.CreateComponentScope(GetType().Name);
            Messenger = messenger;
            _heartBeatPeriod = heartbeatPeriod ?? TimeSpan.FromSeconds(30);
            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private void ForceStopMessenger(object state)
        {
            Log.WriteWarningAsync(nameof(ForceStopMessenger), "Monitoring heartbeat", $"Heart stopped. Restarting {GetType().Name}").GetAwaiter().GetResult();

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

        private async Task MessageLoopImpl()
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
            finally
            {
                try
                {
                    await Messenger.StopAsync(CancellationToken);
                }
                catch (Exception)
                {
                    // Nothing can do here
                }
            }
        }
        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            const int maxAttemptsBeforeLogError = 20;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is AuthenticationException) && !CancellationToken.IsCancellationRequested)
                .WaitAndRetryForeverAsync(attempt =>
                {
                    if (attempt == 1)
                    {
                        Log.WriteWarningAsync(nameof(WebSocketSubscriber), "Receiving messages from the socket", "Unable to establish connection with server. Will retry in 5 secs. ").GetAwaiter().GetResult();
                    }

                    if (attempt % maxAttemptsBeforeLogError == 0)
                    {
                        Log.WriteErrorAsync(nameof(WebSocketSubscriber), "Receiving messages from the socket", new Exception($"Unable to recover the connection after { maxAttemptsBeforeLogError } attempts. Will try in 5 min.")).GetAwaiter().GetResult();
                    }
                    return attempt % maxAttemptsBeforeLogError == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout);
                }); // After every maxAttemptsBeforeLogError attempts wait 5min 

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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _heartBeatMonitoringTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    await Log.WriteWarningAsync(nameof(MessageLoopImpl), GetType().Name, $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                    throw;
                }
            });
        }


        private void StartImpl()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").GetAwaiter().GetResult();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask?.Dispose();
            _messageLoopTask = Task.Run(MessageLoop, _cancellationTokenSource.Token);
        }

        private void StopImpl()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").GetAwaiter().GetResult();
            _cancellationTokenSource?.Cancel();
            try
            {
                _messageLoopTask?.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.WriteInfoAsync("Stopping", ex.Message, $"Exception was thrown while stopping. Ignore it. {ex}").GetAwaiter().GetResult();
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
            Dispose(true);
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
