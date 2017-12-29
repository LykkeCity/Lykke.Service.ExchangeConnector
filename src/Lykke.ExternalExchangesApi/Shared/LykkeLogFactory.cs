using QuickFix;

namespace Lykke.ExternalExchangesApi.Shared
{
    public sealed class LykkeLogFactory : ILogFactory
    {
        private readonly global::Common.Log.ILog lykkeLog;
        private readonly bool _logIncoming;
        private readonly bool _logOutgoing;
        private readonly bool _logEvent;

        public LykkeLogFactory(global::Common.Log.ILog lykkeLog, bool logIncoming = true, bool logOutgoing = true, bool logEvent = true)
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
