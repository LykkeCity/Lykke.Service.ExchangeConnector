using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal sealed class OrderBookSnapshot
    {
        public string Source { get; }

        public string AssetPair { get; }

        public DateTime InternalTimestamp { get; }

        public DateTime OrderBookTimestamp { get; }

        public IDictionary<string, OrderBookItem> Asks { get; }

        public IDictionary<string, OrderBookItem> Bids { get; }

        public string GeneratedId { get; internal set; }

        public OrderBookSnapshot(string source, string assetPair, DateTime orderBookTimestamp)
        {
            InternalTimestamp = DateTime.UtcNow;
            Source = source;
            AssetPair = assetPair;
            Asks = new Dictionary<string, OrderBookItem>();
            Bids = new Dictionary<string, OrderBookItem>();
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

        public void AddOrUpdateOrders(IEnumerable<OrderBookItem> newOrders)
        {
            foreach (var order in newOrders)
            {
                if (order.IsBuy)
                    Bids[order.Id] = order;
                else
                    Asks[order.Id] = order;
            }
        }

        public void DeleteOrders(IReadOnlyCollection<OrderBookItem> orders)
        {
            foreach (var order in orders)
            {
                if (order.IsBuy)
                    Bids.Remove(order.Id, out var _);
                else
                    Asks.Remove(order.Id, out var _);
            }
        }
    }
}
