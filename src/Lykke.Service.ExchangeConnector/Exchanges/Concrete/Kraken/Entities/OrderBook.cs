using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.ExternalExchangesApi.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class OrderBook
    {
        public string Instrument { get; set; }

        [JsonProperty("asks")]
        public List<decimal[]> RawAsks { get; set; }

        [JsonProperty("bids")]
        public List<decimal[]> RawBids { get; set; }

        public IEnumerable<OrderBookItem> Asks =>
            RawAsks.Select(x => new OrderBookItem(x));

        public IEnumerable<OrderBookItem> Bids =>
            RawBids.Select(x => new OrderBookItem(x));
    }

    public class OrderBookItem
    {
        public OrderBookItem(decimal[] values)
        {
            Price = values[0];
            Volume = values[1];
            Time = DateTimeUtils.FromUnix((long)values[2]);
        }

        public DateTime Time { get; set; }

        public decimal Price { get; set; }

        public decimal Volume { get; set; }
    }
}
