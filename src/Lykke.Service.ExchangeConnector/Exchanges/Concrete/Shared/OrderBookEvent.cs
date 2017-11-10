using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class OrderBookEvent
    {
        public OrderBookEventType EventType { get; set; }

        public ICollection<OrderBookItem> Items { get; set; }

        public Guid SnapshotId { get; set; }
    }
}
