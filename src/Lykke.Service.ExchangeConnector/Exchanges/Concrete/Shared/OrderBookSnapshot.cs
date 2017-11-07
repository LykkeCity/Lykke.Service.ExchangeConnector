using System;
using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public sealed class OrderBookSnapshot
    {
        public OrderBookSnapshot(string source, string assetPairId, DateTime timestamp,
            IReadOnlyCollection<OrderBookItem> asks, IReadOnlyCollection<OrderBookItem> bids)
        {
            Source = source;
            AssetPairId = assetPairId;
            Asks = asks;
            Bids = bids;
            Timestamp = timestamp;
        }

        public string Source { get; }

        public string AssetPairId { get; }

        public DateTime Timestamp { get; }

        public IReadOnlyCollection<OrderBookItem> Asks { get; }

        public IReadOnlyCollection<OrderBookItem> Bids { get; }
    }
}
