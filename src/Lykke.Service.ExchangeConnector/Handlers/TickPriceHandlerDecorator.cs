using System.Threading.Tasks;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal class TickPriceHandlerDecorator : IHandler<TickPrice>
    {
        private readonly IHandler<TickPrice> _rabbitMqHandler;

        public TickPriceHandlerDecorator(IHandler<TickPrice> rabbitMqHandler)
        {
            _rabbitMqHandler = rabbitMqHandler;
        }

        public async Task Handle(TickPrice message)
        {
            await _rabbitMqHandler.Handle(message);
        }
    }
}
