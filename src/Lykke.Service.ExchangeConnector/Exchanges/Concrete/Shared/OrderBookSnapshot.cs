using System;
using System.Collections.Generic;

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

        public void AddOrUpdateOrders(IEnumerable<OrderBookItem> newOrders)
        {
            foreach (var order in newOrders)
            {
                if (order.IsBuy)
                {
                    if (Bids.TryGetValue(order.Id, out var storedOrder) && order.Price == 0) // Updates from some exchanges don't have price
                    {
                        order.Price = storedOrder.Price;
                    }
                    Bids[order.Id] = order;
                }
                else
                {
                    if (Asks.TryGetValue(order.Id, out var storedOrder) && order.Price == 0) // Updates from some exchanges don't have price
                    {
                        order.Price = storedOrder.Price;
                    }
                    Asks[order.Id] = order;
                }
            }
        }

        public void DeleteOrders(IEnumerable<OrderBookItem> orders)
        {
            foreach (var order in orders)
            {
                if (order.IsBuy)
                    Bids.Remove(order.Id);
                else
                    Asks.Remove(order.Id);
            }
        }
    }
}
