using System.Threading.Tasks;

namespace TradingBot.Handlers
{
    internal interface IHandler<in T>
    {
        Task Handle(T message);
    }
}
