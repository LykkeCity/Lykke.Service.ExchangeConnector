using System;
using System.Collections.Generic;

namespace TradingBot.FixConnector
{
    public class FullOrderBook
    {
        public FullOrderBook(string source, string asset, DateTime timestamp, List<PriceVolume> asks, List<PriceVolume> bids)
        {
            Source = source;
            Asset = asset;
            Timestamp = timestamp;
            Asks = asks;
            Bids = bids;
        }

        public string Source { get; }

        public string Asset { get; }

        public DateTime Timestamp { get; }

        public List<PriceVolume> Asks { get; }

        public List<PriceVolume> Bids { get; }
    }
}
