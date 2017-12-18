using System.Collections.Generic;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public sealed class GdaxOrderBook
    {
        public long Sequence { get; set; }

        public ICollection<GdaxOrderBookEntityRow> Bids { get; set; }

        public ICollection<GdaxOrderBookEntityRow> Asks { get; set; }
    }
}
