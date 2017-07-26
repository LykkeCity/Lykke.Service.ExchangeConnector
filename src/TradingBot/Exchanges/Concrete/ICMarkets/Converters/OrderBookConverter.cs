using System.Linq;
using TradingBot.Common.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.ICMarkets.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.ICMarkets.Converters
{
    public static class OrderBookConverter
    {
        public static InstrumentTickPrices ToInstrumentTickPrices(this OrderBook orderBook)
        {
            return new InstrumentTickPrices(new Instrument(ICMarketsExchange.Name, orderBook.Asset), 
                    orderBook.Asks.Zip(orderBook.Bids, (ask, bid) => new TickPrice(orderBook.Timestamp, ask.Price, bid.Price)).ToArray()
                );
        }
    }
}