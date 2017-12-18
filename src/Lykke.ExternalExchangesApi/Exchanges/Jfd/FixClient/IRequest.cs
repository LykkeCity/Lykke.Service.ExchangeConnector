
using QuickFix;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    internal interface IRequest
    {
        Message Message { get; }
        void Reject(string reason);
    }
}
