using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Polly;
using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Infrastructure.WebSockets
{
    class WebSocketSubscriber : IDisposable
    {
        protected readonly ILog Log = null;
        protected readonly WebSocketTextMessenger Messenger;
        protected Func<string, Task> handler;
        private Task _messageLoopTask;
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken CancellationToken;

        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod = TimeSpan.FromSeconds(3000);

        public WebSocketSubscriber(string uri, ILog log)
        {
            Log = log.CreateComponentScope(GetType().Name);
            Messenger = new WebSocketTextMessenger(uri, Log);
            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private async void ForceStopMessenger(object state)
        {
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

        public WebSocketSubscriber Subscribe(Func<string, Task> handler)
        {
            this.handler = handler;
            return this;
        }

        protected virtual async Task HandleResponse(string json, CancellationToken token)
        {
            if (this.handler != null)
            {
                try
                {
                    await this.handler(json);
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(WebSocketSubscriber), $"An exception occurred while handling message: '{json}'", ex);
                }
            }
        }

        public virtual void Start()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask = Task.Run(async () => await MessageLoop());
        }

        public virtual void Stop()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").Wait();
            _cancellationTokenSource?.Cancel();
            _messageLoopTask.Wait();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop();
            Messenger?.Dispose();
            _messageLoopTask?.Dispose();
            _cancellationTokenSource?.Dispose();
            _heartBeatMonitoringTimer?.Dispose();
        }

        protected virtual Task Connect(CancellationToken token)
        {
            return Messenger.ConnectAsync(token);
        }

        protected virtual async Task MessageLoopImpl()
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
        }

        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryForeverAsync(attempt => attempt % 60 == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout)); // After every 60 attempts wait 5min 

            await retryPolicy.ExecuteAsync(async () =>
            {
                await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                try
                {
                    await MessageLoopImpl();
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(MessageLoopImpl), $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                    throw;
                }
            });
        }
    }
}
