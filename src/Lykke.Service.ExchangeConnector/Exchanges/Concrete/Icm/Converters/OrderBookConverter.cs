using System.Linq;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm.Converters
{
    public static class OrderBookConverter
    {
        public static InstrumentTickPrices ToInstrumentTickPrices(this OrderBook orderBook)
        {
            return new InstrumentTickPrices(new Instrument(IcmExchange.Name, orderBook.Asset), 
                    orderBook.Asks.Zip(orderBook.Bids, (ask, bid) => new TickPrice(orderBook.Timestamp, ask.Price, bid.Price)).ToArray()
                );
        }
    }
}