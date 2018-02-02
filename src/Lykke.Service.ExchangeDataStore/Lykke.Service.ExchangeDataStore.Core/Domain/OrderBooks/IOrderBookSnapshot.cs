using System;
using System.Collections.Generic;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public interface IOrderBookSnapshot
    {
        string Source { get; }

        string AssetPair { get; }

        DateTime Timestamp { get; }

        IReadOnlyCollection<OrderBookItem> Asks { get; }

        IReadOnlyCollection<OrderBookItem> Bids { get; }

        string GeneratedId { get; set; }
   }
}
