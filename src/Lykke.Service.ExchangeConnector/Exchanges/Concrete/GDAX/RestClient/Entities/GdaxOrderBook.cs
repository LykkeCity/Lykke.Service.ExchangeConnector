using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities
{
    internal sealed class GdaxOrderBook
    {
        public long Sequence { get; set; }

        public ICollection<GdaxOrderBookEntityRow> Bids { get; set; }

        public ICollection<GdaxOrderBookEntityRow> Asks { get; set; }
    }
}
