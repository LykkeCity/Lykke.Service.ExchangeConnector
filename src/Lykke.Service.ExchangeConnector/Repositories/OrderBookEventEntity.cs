using System;
using TradingBot.Helpers;

namespace TradingBot.Repositories
{
    public class OrderBookEventEntity: BaseEntity
    {
        public string OrderBookSnapshotId { get; set; }

        public int EventType { get; set; }

        public DateTime OrderEventTimestamp { get; set; }

        public string OrderId { get; set; }

        public string Symbol { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public bool IsBuy { get; set; }

        public OrderBookEventEntity()
        {
        }

        public OrderBookEventEntity(string orderBookSnapshotId, DateTime orderEventTimestamp, string orderId)
        {
            OrderBookSnapshotId = orderBookSnapshotId;
            OrderEventTimestamp = orderEventTimestamp;
            OrderId = orderId;

            PartitionKey = orderBookSnapshotId.RemoveSpecialCharacters('-', '_', '.');
            RowKey = $"{Guid.NewGuid()}"
                .RemoveSpecialCharacters('-', '_', '.');
        }
    }
}
