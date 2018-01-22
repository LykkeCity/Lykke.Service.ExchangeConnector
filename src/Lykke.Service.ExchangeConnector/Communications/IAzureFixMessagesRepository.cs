using QuickFix;

namespace TradingBot.Communications
{
    internal interface IAzureFixMessagesRepository
    {
        void SaveMessage(Message message, FixMessageDirection direction);
    }
}