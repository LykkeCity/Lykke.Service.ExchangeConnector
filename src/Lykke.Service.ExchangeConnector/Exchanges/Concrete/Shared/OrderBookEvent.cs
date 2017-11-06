using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class OrderBookEvent
    {
        public OrderAction Action { get; set; }

        public ICollection<OrderBookItem> Items { get; set; }

        public Guid SnapshotId { get; set; }
    }
}
