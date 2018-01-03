using System;
using System.Collections.Generic;
using Common.Log;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using QuickFix.Transport;
using ILog = Common.Log.ILog;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Shared
{
    public abstract class FixTradeSessionConnector : IApplication, IDisposable
    {
        private readonly FixConnectorConfiguration _config;
        protected readonly ILog Log;
        private SessionID _sessionId;
        private readonly SocketInitiator _socketInitiator;
        private readonly Dictionary<int, IRequest> _rejectList = new Dictionary<int, IRequest>();
        protected readonly List<IMessageHandler> Handlers = new List<IMessageHandler>();
        public FixConnectorState State { get; private set; }


        protected readonly object RejectLock = new object();

        protected FixTradeSessionConnector(FixConnectorConfiguration config, ILog log)
        {
            _config = config;
            Log = log.CreateComponentScope(GetType().Name);
            var settings = new SessionSettings(config.FixConfig);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(Log, false, false);
            _socketInitiator = new SocketInitiator(this, storeFactory, settings, logFactory);
        }

        public void ToAdmin(Message message, SessionID sessionId)
        {
            if (message is Logon logon)
            {
                logon.Password = new Password(_config.Password);
                State = FixConnectorState.Connecting;
            }
            else if (message is Logout logout)
            {
                State = FixConnectorState.Disconnecting;
                if (logout.IsSetText())
                {
                    Log.WriteInfoAsync(nameof(Logout), string.Empty, logout.Text.Obj).GetAwaiter().GetResult(); ;
                }
            }
        }

        public void FromAdmin(Message message, SessionID sessionId)
        {
            if (message is Reject reject)
            {
                lock (RejectLock)
                {
                    var seqNum = reject.RefSeqNum.Obj;
                    if (_rejectList.TryGetValue(seqNum, out var request))
                    {
                        var reason = reject.IsSetText() ? reject.Text.Obj : "Request rejected. No additional information";
                        request.Reject(reason);
                        _rejectList.Remove(seqNum);
                    }
                }
            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            var seqNum = message.Header.GetField(new MsgSeqNum()).Obj;
            lock (RejectLock)
            {
                if (_rejectList.ContainsKey(seqNum))
                {
                    _rejectList.Remove(seqNum);
                }
            }
        }


        public void FromApp(Message message, SessionID sessionId)
        {
            foreach (var messageHandler in Handlers)
            {
                if (messageHandler.HandleMessage(message))
                {
                    break;
                }
            }
        }
        public void OnCreate(SessionID sessionId)
        {
            // Nothing to do here
        }

        public void OnLogout(SessionID sessionId)
        {
            State = FixConnectorState.Disconnected;
        }

        public void OnLogon(SessionID sessionId)
        {
            _sessionId = sessionId;
            OnConnected();
        }

        private void OnConnected()
        {
            State = FixConnectorState.Connected;
        }

        protected void RegisterForRejectResponse(IRequest request)
        {
            var seqNum = request.Message.Header.GetField(new MsgSeqNum()).Obj;
            lock (RejectLock)
            {
                _rejectList[seqNum] = request;
            }
        }

        protected void EnsureCanHandleRequest()
        {
            if (State != FixConnectorState.Connected)
            {
                throw new InvalidOperationException($"Can't handle request. Connector state {State}");
            }
        }

        protected void SendRequest(Message request)
        {
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
            Log.WriteInfoAsync(nameof(Start), string.Join("/n", _config.FixConfig), "Starting fix connection with configuration").Wait();
            _socketInitiator.Start();
            Log.WriteInfoAsync(nameof(Start), string.Empty, "Fix connection started").Wait();
        }

        public void Stop()
        {
            lock (RejectLock)
            {
                foreach (var request in _rejectList)
                {
                    request.Value.Reject("Connector closed");
                }
            }
            _socketInitiator?.Stop();
        }

        public virtual void Dispose()
        {
            Stop();
            _socketInitiator?.Dispose();
        }
    }
}
