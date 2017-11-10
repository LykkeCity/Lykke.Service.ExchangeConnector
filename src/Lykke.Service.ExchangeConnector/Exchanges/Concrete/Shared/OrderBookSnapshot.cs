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

        public DateTime Timestamp { get; }

        public ConcurrentDictionary<string, OrderBookItem> Asks { get; }

        public ConcurrentDictionary<string, OrderBookItem> Bids { get; }

        public OrderBookSnapshot(string source, string assetPair, DateTime timestamp)
        {
            Source = source;
            AssetPair = assetPair;
            Asks = new ConcurrentDictionary<string, OrderBookItem>();
            Bids = new ConcurrentDictionary<string, OrderBookItem>();
            Timestamp = timestamp;
        }

        public OrderBookSnapshot(string source, string assetPair, DateTime timestamp,
            IEnumerable<OrderBookItem> asks, IEnumerable<OrderBookItem> bids) 
            : this(source, assetPair, timestamp)
        {
            foreach (var ask in asks)
                Asks[ask.Id] = ask;

            foreach (var bid in bids)
                Bids[bid.Id] = bid;
        }
    }
}
