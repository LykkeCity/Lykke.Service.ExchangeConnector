using QuickFix;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class LykkeQuickFixLog : ILog
    {
        private readonly Common.Log.ILog _lykkeLog;
        private readonly SessionID _sessionId;
        private readonly bool _logIncoming;
        private readonly bool _logOutgoing;
        private readonly bool _logEvents;

        public LykkeQuickFixLog(Common.Log.ILog lykkeLog, SessionID sessionId, bool logIncoming, bool logOutgoing, bool logEvents)
        {
            _lykkeLog = lykkeLog;
            _sessionId = sessionId;
            _logIncoming = logIncoming;
            _logOutgoing = logOutgoing;
            _logEvents = logEvents;
        }

        public void Dispose()
        {
        }

        public void Clear()
        {
        }

        public void OnIncoming(string msg)
        {
            if (_logIncoming)
            {
                _lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnIncoming), _sessionId.ToString(), msg).Wait();
            }
        }

        public void OnOutgoing(string msg)
        {
            if (_logOutgoing)
            {
                _lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnOutgoing), _sessionId.ToString(), msg).Wait();
            }
        }

        public void OnEvent(string s)
        {
            if (_logEvents)
            {
                _lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnEvent), _sessionId.ToString(), s).Wait();
            }
        }
    }
}
