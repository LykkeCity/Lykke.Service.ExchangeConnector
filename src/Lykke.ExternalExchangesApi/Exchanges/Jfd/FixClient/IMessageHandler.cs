using QuickFix;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    internal interface IMessageHandler
    {
        bool HandleMessage(Message message);
        void RejectMessage(string id);
    }
}
