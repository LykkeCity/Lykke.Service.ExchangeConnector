using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public sealed class OrderBookSnapshot
    {
        public string Source { get; }

        public string AssetPair { get; }

        public DateTime InternalTimestamp { get; }

        public DateTime OrderBookTimestamp { get; }

        public ConcurrentDictionary<string, OrderBookItem> Asks { get; }

        public ConcurrentDictionary<string, OrderBookItem> Bids { get; }

        public string GeneratedId { get; internal set; }

        public OrderBookSnapshot(string source, string assetPair, DateTime orderBookTimestamp)
        {
            InternalTimestamp = DateTime.UtcNow;
            Source = source;
            AssetPair = assetPair;
            Asks = new ConcurrentDictionary<string, OrderBookItem>();
            Bids = new ConcurrentDictionary<string, OrderBookItem>();
            OrderBookTimestamp = orderBookTimestamp;
        }

        public OrderBookSnapshot(string source, string assetPair, DateTime orderBookTimestamp,
            IEnumerable<OrderBookItem> asks, IEnumerable<OrderBookItem> bids) 
            : this(source, assetPair, orderBookTimestamp)
        {
            foreach (var ask in asks)
                Asks[ask.Id] = ask;

            foreach (var bid in bids)
                Bids[bid.Id] = bid;
        }
    }
}
