using QuickFix;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class LykkeLogFactory : ILogFactory
    {
        private readonly Common.Log.ILog lykkeLog;

        public LykkeLogFactory(Common.Log.ILog lykkeLog)
        {
            this.lykkeLog = lykkeLog;
        }
        
        public ILog Create(SessionID sessionId)
        {
            return new LykkeQuickFixLog(lykkeLog, sessionId);
        }
    }
}