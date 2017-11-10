using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using WampSharp.Binding;
using WampSharp.Core.Listener;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Realm;
using WampSharp.WebSockets;
using Newtonsoft.Json.Linq;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Infrastructure.Wamp
{
    public class WampSubscriber<T> : ProducerConsumer<T>, IStartable, IStopable where T : class
    {
        private IWampChannel channel = null;
        private List<IDisposable> subscriptions = new List<IDisposable>();
        private ILog log = null;
        private WampSubscriberSettings settings;
        private Func<T, Task> handlers;
        private static readonly object _sync = new object();
        private bool isStarted = false;
        private bool isConnected = false;
        private bool isDisposed = false;
        private Task thread = null;
        private TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
        private CancellationTokenSource cancelToken = new CancellationTokenSource();

        public WampSubscriber(WampSubscriberSettings settings, ILog log)
            : base(log)
        {
            if (settings == null) { throw new ArgumentNullException(nameof(settings)); }
            this.settings = settings;
            this.log = log;
        }

        public override void Start()
        {
            ValidateInstance();

            if (this.isStarted)
                return;

            lock (_sync)
            {
                if (this.isStarted)
                    return;

                this.isStarted = true;
            }

            base.Start();

            this.cancelToken = new CancellationTokenSource();
            this.thread = this.OpenConnectionAndSub();
        }

        public override void Stop()
        {
            ValidateInstance();

            if (!this.isStarted)
                return;

            lock (_sync)
            {
                if (!this.isStarted)
                    return;
                this.isStarted = false;
            }

            base.Stop();

            this.cancelToken.Cancel();
            this.tcs?.SetResult(0);
            this.thread.Wait();
        }

        public virtual WampSubscriber<T> Subscribe(Func<T, Task> callback)
        {
            ValidateInstance();
            this.handlers += callback;
            return this;
        }

        private async Task OpenConnectionAndSub()
        {
            await Task.Run(async () =>
            {
                // Connect
                // Subscribe
                // Await
                // Unsub

                while (!this.cancelToken.IsCancellationRequested)
                {
                    this.tcs = new TaskCompletionSource<int>();
                    this.isConnected = false;

                    // Connect
                    // 
                    var binding = new JTokenJsonBinding();
                    Func<IControlledWampConnection<JToken>> connectionFactory =
                            () => new ControlledTextWebSocketConnection<JToken>(new Uri(this.settings.Address), binding);

                    this.channel = new WampChannelFactory().CreateChannel(this.settings.Realm, connectionFactory, binding);

                    var monitor = this.channel.RealmProxy.Monitor;
                    monitor.ConnectionBroken += OnConnectionBroken;
                    monitor.ConnectionError += OnConnectionError;

                    while (!monitor.IsConnected && !this.cancelToken.IsCancellationRequested)
                    {
                        try
                        {
                            this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Trying to connect to server: {this.settings.Address} realm: {this.settings.Realm}");
                            await this.channel.Open();
                        }
                        catch (Exception ex)
                        {
                            this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, ex);
                            this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, "Retrying to connect in 5 sec...");
                            await Task.Delay(5000);
                        }
                    }

                    // Subscribe to topics
                    //
                    if (!this.cancelToken.IsCancellationRequested)
                    {
                        this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Connected to server {this.settings.Address}");

                        foreach (var topic in this.settings.Topics)
                        {
                            var subj = this.channel.RealmProxy.Services.GetSubject<T>(topic);
                            this.subscriptions.Add(subj.Subscribe(this.OnNext, this.OnError, this.OnCompleted));
                            this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Subscribed to topic '${topic}'.");
                        }

                        this.isConnected = true;

                        await tcs.Task;
                    }

                    // Unsub
                    CloseConnection();
                }
            });
        }

        private void OnConnectionError(object sender, WampConnectionErrorEventArgs e)
        {
            this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(OnConnectionError), string.Empty, e?.Exception);
            Reconnect();
        }

        private void OnConnectionBroken(object sender, WampSessionCloseEventArgs e)
        {
            this.log?.WriteWarningAsync(nameof(WampSubscriber<T>), nameof(OnConnectionBroken), string.Empty,
                $"Connection is broken. Reason: {e?.Reason}. Message: {e?.Details?.Message}");
            Reconnect();
        }

        private void Reconnect()
        {
            if (this.isConnected)
            {
                lock (_sync)
                {
                    if (this.isConnected)
                    {
                        this.isConnected = false;
                        this.tcs.SetResult(0);
                    }
                }
            }
        }

        protected virtual void OnNext(T item)
        {
            this.Produce(item);
        }

        protected virtual void OnError(Exception ex)
        {
            this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(OnError), string.Empty, ex);
        }

        protected virtual void OnCompleted()
        {
            this.log?.WriteWarningAsync(nameof(WampSubscriber<T>), nameof(OnCompleted), string.Empty, "Subscription is completed.");
        }

        public new void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.Stop();
                this.isDisposed = true;
            }
        }

        private void ValidateInstance()
        {
            if (this.isDisposed) { throw new InvalidOperationException("Calling disposed instance."); }
        }

        private void CloseConnection()
        {
            this.subscriptions.ForEach(i => i.Dispose());
            this.subscriptions.Clear();

            if (this.channel != null)
            {
                var monitor = this.channel.RealmProxy.Monitor;
                monitor.ConnectionBroken -= OnConnectionBroken;
                monitor.ConnectionError -= OnConnectionError;

                this.channel.Close();
                this.channel = null;
            }
        }

        protected override async Task Consume(T item)
        {
            foreach (Func<T, Task> handler in this.handlers.GetInvocationList())
            {
                try
                {
                    await handler(item);
                }
                catch (Exception ex)
                {
                    this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(OnError), "Message handler executed with exception.", ex);
                }
            }
        }
    }
}
