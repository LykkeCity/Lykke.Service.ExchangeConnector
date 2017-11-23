using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Transport;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using ILog = Common.Log.ILog;
using Message = QuickFix.Message;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal sealed class JfdQuotesSessionConnector : IApplication, IMessenger<MarketDataRequest, Message>
    {
        private readonly JfdExchangeConfiguration _config;
        private readonly ILog _log;
        private SessionID _sessionId;
        private readonly SocketInitiator _socketInitiator;
        private TaskCompletionSource<Message> _requestRejectedCompletionSource;
        private TaskCompletionSource<bool> _connectionCompletionSource;
        private readonly BlockingCollection<Message> _responsesQueue = new BlockingCollection<Message>();

        public JfdConnectorState State { get; private set; }



        public JfdQuotesSessionConnector(JfdExchangeConfiguration config, ILog log)
        {
            _config = config;
            _log = log.CreateComponentScope(GetType().Name);
            var settings = new SessionSettings(config.GetQuotingFixConfigAsReader());
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(_log, false, false);
            _socketInitiator = new SocketInitiator(this, storeFactory, settings, logFactory);

        }



        public void ToAdmin(Message message, SessionID sessionId)
        {
            if (message is Logon logon)
            {
                logon.Password = new Password(_config.Password);
                State = JfdConnectorState.Connecting;
            }
            else if (message is Logout)
            {
                State = JfdConnectorState.Disconnecting;
            }
        }

        public void FromAdmin(Message message, SessionID sessionId)
        {
            if (message is TestRequest)
            {
                _responsesQueue.Add(message, CancellationToken.None);
            }
            else if (message is Reject reject)
            {
                var reason = reject.IsSetText() ? reject.Text.Obj : "Request rejected. No additional information";
                _requestRejectedCompletionSource.TrySetException(new OperationRejectedException(reason));
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
                _requestRejectedCompletionSource.TrySetException(new OperationRejectedException(reason)); // Hope someone has already waited on a task.
            }
        }

        public void OnCreate(SessionID sessionId)
        {
            // Nothing to do here
        }

        public void OnLogout(SessionID sessionId)
        {
            if (State == JfdConnectorState.Connecting)
            {
                _connectionCompletionSource.TrySetException(new OperationRejectedException("Logon rejected. See the log for details"));
            }
            else
            {
                State = JfdConnectorState.Disconnected;
            }
        }

        public void OnLogon(SessionID sessionId)
        {
            _sessionId = sessionId;
            State = JfdConnectorState.Connected;
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
            if (State != JfdConnectorState.Connected)
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
            if (!(State == JfdConnectorState.Disconnected || State == JfdConnectorState.NotConnected))
            {
                throw new InvalidOperationException($"Unable to connect. Current state {State}");
            }
            _connectionCompletionSource = new TaskCompletionSource<bool>();
            SubscribeOnCancelEvent(_connectionCompletionSource, cancellationToken);

            _log.WriteInfoAsync(nameof(ConnectAsync), string.Join("/n", _config.QuotingFixConfiguration), "Starting fix connection with configuration").GetAwaiter().GetResult();
            _socketInitiator.Start();
            _log.WriteInfoAsync(nameof(ConnectAsync), string.Empty, "Fix connection started").GetAwaiter().GetResult();
            return _connectionCompletionSource.Task;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (State == JfdConnectorState.Connecting || State == JfdConnectorState.Connected)
            {
                _socketInitiator?.Stop();
            }
            State = JfdConnectorState.Disconnected;
            return Task.CompletedTask;
        }
    }
}
