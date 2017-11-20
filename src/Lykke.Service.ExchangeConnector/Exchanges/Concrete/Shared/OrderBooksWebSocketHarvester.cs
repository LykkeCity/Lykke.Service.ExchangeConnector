using Common.Log;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksWebSocketHarvester : OrderBooksHarvesterBase
    {
        protected WebSocketTextMessenger Messenger;

        protected OrderBooksWebSocketHarvester(string exchangeName, IExchangeConfiguration exchangeConfiguration, string uri, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(exchangeName, exchangeConfiguration, log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            Messenger = new WebSocketTextMessenger(uri, log, CancellationToken);
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
