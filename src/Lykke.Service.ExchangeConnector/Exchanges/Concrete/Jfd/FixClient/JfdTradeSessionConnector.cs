using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Transport;
using TradingBot.Exchanges.Concrete.Icm;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;
using ILog = Common.Log.ILog;
using Message = QuickFix.Message;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal sealed class JfdTradeSessionConnector : IApplication, IDisposable
    {
        private readonly JfdExchangeConfiguration _config;
        private readonly ILog _log;
        private SessionID _sessionId;
        private readonly SocketInitiator _socketInitiator;
        private readonly OrdersHandler _ordersHandler;
        private readonly PositionsHandler _positionsHandler;
        private readonly CollateralHandler _collateralHandler;
        private readonly List<IMessageHandler> _handlers = new List<IMessageHandler>();
        private readonly Dictionary<int, IRequest> _rejectList = new Dictionary<int, IRequest>();
        private readonly object _rejectLock = new object();

        public JfdConnectorState State { get; private set; }



        public JfdTradeSessionConnector(JfdExchangeConfiguration config, ILog log)
        {
            _config = config;
            _log = log.CreateComponentScope(nameof(JfdTradeSessionConnector));
            var settings = new SessionSettings(config.GetTradingFixConfigAsReader());
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(_log, false, false);
            _socketInitiator = new SocketInitiator(this, storeFactory, settings, logFactory);
            _ordersHandler = new OrdersHandler(log);
            _positionsHandler = new PositionsHandler(log);
            _collateralHandler = new CollateralHandler(log);
            _handlers.Add(_ordersHandler);
            _handlers.Add(_positionsHandler);
            _handlers.Add(_collateralHandler);

        }
        public void ToAdmin(Message message, SessionID sessionId)
        {
            if (MsgType.LOGON == message.Header.GetString(Tags.MsgType))
            {
                message.SetField(new Password(_config.Password));
                State = JfdConnectorState.Connecting;
            }
            if (MsgType.LOGOUT == message.Header.GetString(Tags.MsgType))
            {
                State = JfdConnectorState.Disconnecting;
            }
        }

        public void FromAdmin(Message message, SessionID sessionId)
        {
            var msgType = message.Header.GetField(new MsgType()).Obj;
            if (MsgType.REJECT.Equals(msgType))
            {
                lock (_rejectLock)
                {
                    var seqNum = message.Header.GetField(new MsgSeqNum()).Obj;
                    if (_rejectList.TryGetValue(seqNum, out var request))
                    {
                        var reason = message.IsSetField(new Text()) ? message.GetField(new Text()).Obj : "Request rejected. No additional information";
                        request.Reject(reason);
                        _rejectList.Remove(seqNum);
                    }
                }
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            var seqNum = message.Header.GetField(new MsgSeqNum()).Obj;
            lock (_rejectLock)
            {
                if (_rejectList.ContainsKey(seqNum))
                {
                    _rejectList.Remove(seqNum);
                }
            }
        }

        public void FromApp(Message message, SessionID sessionId)
        {
            foreach (var messageHandler in _handlers)
            {
                if (messageHandler.HandleMessage(message))
                {
                    break;
                }
            }
        }

        public void OnCreate(SessionID sessionId)
        {

        }

        public void OnLogout(SessionID sessionId)
        {
            State = JfdConnectorState.Disconnected;
        }

        public void OnLogon(SessionID sessionId)
        {
            _sessionId = sessionId;
            OnConnected();
        }

        private void OnConnected()
        {
            State = JfdConnectorState.Connected;
        }

        public Task<ExecutionReport> AddOrderAsync(NewOrderSingle order, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            lock (_rejectLock)
            {
                var request = _ordersHandler.RegisterMessage(order, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }

        public Task<IReadOnlyCollection<PositionReport>> GetPositionsAsync(RequestForPositions positionRequest, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            lock (_rejectLock)
            {
                var request = _positionsHandler.RegisterMessage(positionRequest, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }

        public Task<IReadOnlyCollection<CollateralReport>> GetCollateralAsync(CollateralInquiry collateralInquiry, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();

            lock (_rejectLock)
            {
                var request = _collateralHandler.RegisterMessage(collateralInquiry, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }

        private void RegisterForRejectResponse(IRequest request)
        {
            var seqNum = request.Message.Header.GetField(new MsgSeqNum()).Obj;
            _rejectList[seqNum] = request;
        }

        private void EnsureCanHandleRequest()
        {
            if (State != JfdConnectorState.Connected)
            {
                throw new InvalidOperationException($"Can't handle request. Connector state {State}");
            }
        }


        private void SendRequest(Message request)
        {
            _log.WriteInfoAsync(nameof(IcmConnector), nameof(SendRequest), string.Empty, $"About to send a request {request}").Wait();

            var header = request.Header;
            header.SetField(new SenderCompID(_sessionId.SenderCompID));
            header.SetField(new TargetCompID(_sessionId.TargetCompID));

            var result = Session.SendToTarget(request);
            if (!result)
            {
                throw new InvalidOperationException("Unable to send request. Unknown error");
            }
        }


        public void Start()
        {
            _log.WriteInfoAsync(nameof(Start), string.Join("/n", _config.TradingFixConfiguration), "Starting fix connection with configuration").Wait();
            _socketInitiator.Start();
            _log.WriteInfoAsync(nameof(Start), string.Empty, "Fix connection started").Wait();
        }

        public void Stop()
        {
            lock (_rejectLock)
            {
                foreach (var request in _rejectList)
                {
                    request.Value.Reject("Connector closed");
                }
            }
            _socketInitiator?.Stop();
        }

        public void Dispose()
        {
            Stop();
            _socketInitiator?.Dispose();
        }
    }
}
