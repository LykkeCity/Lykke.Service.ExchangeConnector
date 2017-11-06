
using QuickFix;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal interface IRequest
    {
        Message Message { get; }
        void Reject(string reason);
    }
}
