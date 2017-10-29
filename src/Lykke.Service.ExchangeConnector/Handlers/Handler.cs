using System.Threading.Tasks;

namespace TradingBot.Handlers
{
    public abstract class Handler<T>
    {
        public abstract Task Handle(T message);
    }
}