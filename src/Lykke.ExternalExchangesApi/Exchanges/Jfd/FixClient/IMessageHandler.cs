using QuickFix;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal interface IMessageHandler
    {
        bool HandleMessage(Message message);
        void RejectMessage(string id);
    }
}
