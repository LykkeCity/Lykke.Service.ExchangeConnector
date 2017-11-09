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
    public class WampSubscriber<T> : IStartable, IStopable where T : class
    {
        private IWampChannel channel = null;
        private List<IDisposable> subscriptions = new List<IDisposable>();
        private ILog log = null;
        private WampSubscriberSettings settings;
        private Thread thread = null;
        private CancellationTokenSource cancelToken = null;
        private bool isDisposed = false;
        private Action<T> handlers;
        private static readonly object _sync = new object();
        private bool isConnected = false;

        public WampSubscriber(WampSubscriberSettings settings)
        {
            if (settings == null) { throw new ArgumentNullException(nameof(settings)); }
            this.settings = settings;
        }

        public virtual void Start()
        {
            ValidateInstance();
            if (this.thread != null) { return; }

            this.cancelToken = new CancellationTokenSource();
            this.thread = new Thread(ReadThread);
            this.thread.Start();
        }

        public virtual void Stop()
        {
            ValidateInstance();

            var t = this.thread;
            var ct = this.cancelToken;
            if (t == null)
                return;

            this.thread = null;
            ct?.Cancel();
            t.Join();
            ct?.Dispose();
        }

        public virtual WampSubscriber<T> Subscribe(Action<T> callback)
        {
            ValidateInstance();
            this.handlers += callback;
            return this;
        }

        public virtual WampSubscriber<T> SetLogger(ILog log)
        {
            ValidateInstance();
            this.log = log;
            return this;
        }

        private void OpenConnectionAndSub()
        {
            // 1. Connect
            // 

            var binding = new JTokenJsonBinding();
            Func<IControlledWampConnection<JToken>> connectionFactory =
                    () => new ControlledTextWebSocketConnection<JToken>(new Uri(this.settings.Address), binding);

            this.channel = new WampChannelFactory().CreateChannel(this.settings.Realm, connectionFactory, binding);

            var monitor = this.channel.RealmProxy.Monitor;
            monitor.ConnectionBroken += OnConnectionBroken;
            monitor.ConnectionError += OnConnectionError;

            while (!monitor.IsConnected)
            {
                if (this.cancelToken.IsCancellationRequested)
                {
                    this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Finishing connecting to server.");
                    return;
                }

                try
                {
                    this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Trying to connect to server: {this.settings.Address} realm: {this.settings.Realm}");
                    this.channel.Open().Wait();
                }
                catch (Exception ex)
                {
                    this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, ex);
                    this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, "Retrying to connect in 5 sec...");
                    Thread.Sleep(5000);
                }
            }
            this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Connected to server {this.settings.Address}");

            // 2. Subscribe to topic
            //
            foreach (var topic in this.settings.Topics)
            {
                var subj = this.channel.RealmProxy.Services.GetSubject<T>(topic);
                this.subscriptions.Add(subj.Subscribe(this.OnNext, this.OnError, this.OnCompleted));
                this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(Start), string.Empty, $"Subscribed to topic '${topic}'.");
            }

            this.isConnected = true;
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

                        CloseConnection();
                        OpenConnectionAndSub();
                    }
                }
            }
        }

        protected virtual void OnNext(T item)
        {
            foreach (Action<T> handler in this.handlers.GetInvocationList())
            {
                try
                {
                    handler(item);
                }
                catch (Exception ex)
                {
                    this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(OnError), "Message handler executed with exception.", ex);
                }
            }
        }

        protected virtual void OnError(Exception ex)
        {
            this.log?.WriteErrorAsync(nameof(WampSubscriber<T>), nameof(OnError), string.Empty, ex);
        }

        protected virtual void OnCompleted()
        {
            this.log?.WriteWarningAsync(nameof(WampSubscriber<T>), nameof(OnCompleted), string.Empty, "Subscription is completed.");
        }

        public void Dispose()
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

        private void ReadThread()
        {
            OpenConnectionAndSub();

            this.cancelToken.Token.WaitHandle.WaitOne();

            CloseConnection();
        }

        private void CloseConnection()
        {
            this.log?.WriteInfoAsync(nameof(WampSubscriber<T>), nameof(CloseConnection), string.Empty, $"Closing connection.");

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
    }
}
