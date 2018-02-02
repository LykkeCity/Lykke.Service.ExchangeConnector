using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lykke.Service.ExchangeDataStore.Services.Domain
{
    internal sealed class OrderBookSnapshot : IOrderBookSnapshot
    {
        public string Source { get; }

        public string AssetPair { get; }

        public DateTime Timestamp { get; }

        public IReadOnlyCollection<OrderBookItem> Asks { get; }

        public IReadOnlyCollection<OrderBookItem> Bids { get; }

        public string GeneratedId { get; set; }


        public OrderBookSnapshot(OrderBook orderBook)
        {
            Timestamp = orderBook.Timestamp;
            Source = orderBook.Source;
            AssetPair = orderBook.AssetPairId;
            Asks = new ReadOnlyCollection<OrderBookItem>(orderBook.Asks.Select(order => order.ToAskOrderBookItem(orderBook.AssetPairId)).ToList());
            Bids = new ReadOnlyCollection<OrderBookItem>(orderBook.Bids.Select(order => order.ToBidOrderBookItem(orderBook.AssetPairId)).ToList());
        }
    }
}
