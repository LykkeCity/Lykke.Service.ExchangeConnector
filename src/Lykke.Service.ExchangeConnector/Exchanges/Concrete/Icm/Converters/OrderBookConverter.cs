using System.Linq;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm.Converters
{
    public static class OrderBookConverter
    {
        public static TickPrice ToInstrumentTickPrices(this OrderBook orderBook)
        {
            return new TickPrice(new Instrument(IcmExchange.Name, orderBook.Asset), 
                    orderBook.Timestamp,
                    orderBook.Asks.Select(x => x.Price).Min(),
                    orderBook.Bids.Select(x => x.Price).Max()
                );
        }
    }
}
