using QuickFix;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal sealed class LykkeLogFactory : ILogFactory
    {
        private readonly Common.Log.ILog lykkeLog;
        private readonly bool _logIncoming;
        private readonly bool _logOutgoing;
        private readonly bool _logEvent;

        public LykkeLogFactory(Common.Log.ILog lykkeLog, bool logIncoming = true, bool logOutgoing = true, bool logEvent = true)
        {
            this.lykkeLog = lykkeLog;
            _logIncoming = logIncoming;
            _logOutgoing = logOutgoing;
            _logEvent = logEvent;
        }

        public ILog Create(SessionID sessionId)
        {
            return new LykkeQuickFixLog(lykkeLog, sessionId, _logIncoming, _logOutgoing, _logEvent);
        }
    }
}
