﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Lykke;
using QuickFix.Transport;
using Message = QuickFix.Message;
using ILog = Common.Log.ILog;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    public sealed class JfdQuotesSessionConnector : IApplication, IMessenger<MarketDataRequest, Message>
    {
        private readonly FixConnectorConfiguration _config;
        private readonly ILog _log;
        private SessionID _sessionId;
        private readonly SocketInitiator _socketInitiator;
        private TaskCompletionSource<Message> _requestRejectedCompletionSource;
        private TaskCompletionSource<bool> _connectionCompletionSource;
        private readonly BlockingCollection<Message> _responsesQueue = new BlockingCollection<Message>();

        public FixConnectorState State { get; private set; }



        public JfdQuotesSessionConnector(FixConnectorConfiguration config, ILog log)
        {
            _config = config;
            _log = log.CreateComponentScope(GetType().Name);
            var settings = new SessionSettings(config.FixConfig);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(_log, false, false);
            _socketInitiator = new SocketInitiator(this, storeFactory, settings, logFactory);
            RechargeQuotesTcs();
        }


        public void ToAdmin(Message message, SessionID sessionId)
        {
            if (message is Logon logon)
            {
                logon.Password = new Password(_config.Password);
                State = FixConnectorState.Connecting;
            }
            else if (message is Logout)
            {
                State = FixConnectorState.Disconnecting;
            }
        }

        public void FromAdmin(Message message, SessionID sessionId)
        {
            if (message is TestRequest || message is Logon)
            {
                _responsesQueue.Add(message, CancellationToken.None);
            }
            else if (message is Reject reject)
            {
                var reason = reject.IsSetText() ? reject.Text.Obj : "Request rejected. No additional information";
                _requestRejectedCompletionSource.TrySetException(new InvalidOperationException(reason));
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {

        }

        public void FromApp(Message message, SessionID sessionId)
        {
            if (message is MarketDataSnapshotFullRefresh)
            {
                _responsesQueue.Add(message, CancellationToken.None);
            }
            if (message is MarketDataRequestReject || message is QuoteCancel)
            {
                var reason = message.IsSetField(new Text()) ? message.GetField(new Text()).Obj : "Request rejected. No additional information";
                _requestRejectedCompletionSource.TrySetException(new InvalidOperationException(reason)); // Hope someone has already waited on a task.
            }
        }

        public void OnCreate(SessionID sessionId)
        {
            // Nothing to do here
        }

        public void OnLogout(SessionID sessionId)
        {
            if (State == FixConnectorState.Connecting)
            {
                _connectionCompletionSource.TrySetException(new InvalidOperationException("Logon rejected. See the log for details"));
            }
            else
            {
                State = FixConnectorState.Disconnected;
            }
        }

        public void OnLogon(SessionID sessionId)
        {
            _sessionId = sessionId;
            State = FixConnectorState.Connected;
            _connectionCompletionSource.TrySetResult(true);
        }


        public Task SendRequestAsync(MarketDataRequest request, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            RechargeQuotesTcs();
            if (!SendRequest(request))
            {
                return Task.FromException(new InvalidOperationException("Unable to send request. Unknown error"));
            }
            return Task.CompletedTask;
        }

        public Task<Message> GetResponseAsync(CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            Task<Message> itemTask;
            if (_responsesQueue.TryTake(out var item))
            {
                itemTask = Task.FromResult(item);
            }
            else
            {
                itemTask = Task.Run(() => _responsesQueue.Take(cancellationToken), cancellationToken);
            }

            return Task.WhenAny(itemTask, _requestRejectedCompletionSource.Task).Unwrap();
        }

        private void RechargeQuotesTcs()
        {
            _requestRejectedCompletionSource = new TaskCompletionSource<Message>();
        }

        private void EnsureCanHandleRequest()
        {
            if (State != FixConnectorState.Connected)
            {
                throw new InvalidOperationException($"Can't handle request. Connector state {State}");
            }
        }

        private void SubscribeOnCancelEvent<T>(TaskCompletionSource<T> tcs, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
        }

        private bool SendRequest(Message request)
        {
            var header = request.Header;
            header.SetField(new SenderCompID(_sessionId.SenderCompID));
            header.SetField(new TargetCompID(_sessionId.TargetCompID));

            var result = Session.SendToTarget(request);
            return result;
        }


        public void Dispose()
        {
            StopAsync(CancellationToken.None);
            _socketInitiator?.Dispose();
            _responsesQueue.Dispose();
        }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (!(State == FixConnectorState.Disconnected || State == FixConnectorState.NotConnected))
            {
                throw new InvalidOperationException($"Unable to connect. Current state {State}");
            }
            _connectionCompletionSource = new TaskCompletionSource<bool>();
            SubscribeOnCancelEvent(_connectionCompletionSource, cancellationToken);

            _log.WriteInfoAsync(nameof(ConnectAsync), string.Join("/n", _config.FixConfig), "Starting fix connection with configuration").GetAwaiter().GetResult();
            _socketInitiator.Start();
            _log.WriteInfoAsync(nameof(ConnectAsync), string.Empty, "Fix connection started").GetAwaiter().GetResult();
            return _connectionCompletionSource.Task;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (State == FixConnectorState.Connecting || State == FixConnectorState.Connected)
            {
                _socketInitiator?.Stop();
            }
            State = FixConnectorState.Disconnected;
            return Task.CompletedTask;
        }
    }
}
