using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal sealed class OrderBookSnapshot
    {
        private readonly ILog _log;
        public string Source { get; }

        public string AssetPair { get; }

        private DateTime InternalTimestamp { get; }

        public DateTime OrderBookTimestamp { get; }

        public IDictionary<string, OrderBookItem> Asks { get; }

        public IDictionary<string, OrderBookItem> Bids { get; }

        public string GeneratedId { get; internal set; }

        private readonly HashSet<string> _invertedAssetIds;

        public OrderBookSnapshot(string source, string assetPair, DateTime orderBookTimestamp, ILog log, IEnumerable<CurrencySymbol> config)
        {
            _log = log;
            InternalTimestamp = DateTime.UtcNow;
            Source = source;
            AssetPair = assetPair;
            Asks = new Dictionary<string, OrderBookItem>();
            Bids = new Dictionary<string, OrderBookItem>();
            OrderBookTimestamp = orderBookTimestamp;
            _invertedAssetIds = config.Where(c => c.OrderBookVolumeInQuoteCcy)
                .Select(c => c.ExchangeSymbol)
                .ToHashSet();
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

                    InvertSize(order);
                    Bids[order.Id] = order;
                }
                else
                {
                    if (Asks.TryGetValue(order.Id, out var storedOrder) && order.Price == 0) // Updates from some exchanges don't have price
                    {
                        order.Price = storedOrder.Price;
                    }
                    InvertSize(order);
                    Asks[order.Id] = order;
                }
            }
        }

        public async Task<bool> DetectNegativeSpread()
        {
            if (Asks.Any() && Bids.Any())
            {
                var bestAsk = Asks.Values.Min(ob => ob.Price);
                var bestBid = Bids.Values.Max(ob => ob.Price);
                if (bestAsk < bestBid)
                {
                    await _log.WriteInfoAsync(nameof(DetectNegativeSpread), $"Exchange {Source} AssetPair {AssetPair} Ask {bestAsk} Bid {bestBid}", "Negative spread detected");
                    return true;
                }
            }

            return false;
        }

        private void InvertSize(OrderBookItem order)
        {
            if (_invertedAssetIds.Contains(order.Symbol))
            {
                order.Size = order.Size / order.Price;
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
