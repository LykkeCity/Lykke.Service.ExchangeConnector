using QuickFix;

namespace Lykke.ExternalExchangesApi.Shared
{
    public interface IMessageHandler
    {
        bool HandleMessage(Message message);
        void RejectMessage(string id);
    }
}
