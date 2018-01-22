
using QuickFix;

namespace Lykke.ExternalExchangesApi.Shared
{
    public interface IRequest
    {
        Message Message { get; }
        void Reject(string reason);
    }
}
