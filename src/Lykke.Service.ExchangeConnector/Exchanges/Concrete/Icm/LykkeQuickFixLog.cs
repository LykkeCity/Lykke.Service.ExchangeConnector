using QuickFix;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class LykkeQuickFixLog : ILog
    {
        private readonly Common.Log.ILog lykkeLog;
        private readonly SessionID sessionId;

        public LykkeQuickFixLog(Common.Log.ILog lykkeLog, SessionID sessionId)
        {
            this.lykkeLog = lykkeLog;
            this.sessionId = sessionId;
        }
        
        public void Dispose()
        {
        }

        public void Clear()
        {
        }

        public void OnIncoming(string msg)
        {
            lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnIncoming), sessionId.ToString(), msg).Wait();
        }

        public void OnOutgoing(string msg)
        {
            lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnOutgoing), sessionId.ToString(), msg).Wait();
        }

        public void OnEvent(string s)
        {
            lykkeLog.WriteInfoAsync(nameof(LykkeQuickFixLog), nameof(OnEvent), sessionId.ToString(), s).Wait();
        }
    }
}