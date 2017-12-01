using Common.Log;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksWebSocketHarvester<TRequest, TResponse> : OrderBooksHarvesterBase
    {
        protected IMessenger<TRequest, TResponse> Messenger;

        protected OrderBooksWebSocketHarvester(string exchangeName, IExchangeConfiguration exchangeConfiguration, IMessenger<TRequest, TResponse> messanger, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(exchangeName, exchangeConfiguration, log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            Messenger = messanger;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (Messenger != null)
                {
                    Messenger.Dispose();
                    Messenger = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
