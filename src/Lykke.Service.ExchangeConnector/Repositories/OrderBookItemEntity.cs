using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Repositories
{
    internal sealed class OrderBookItemEntity : TableEntity, IEquatable<OrderBookItemEntity>
    {
        public string OrderId { get; set; }

        public string Symbol { get; set; }

        public string OrderBookPartitionKey { get; set; }

        public string OrderBookRowKey { get; set; }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public bool IsBuy { get; set; }

        public bool Equals(OrderBookItemEntity other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(PartitionKey, other.PartitionKey) && string.Equals(RowKey, other.RowKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderBookItemEntity) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PartitionKey != null ? PartitionKey.GetHashCode() : 0) * 397) ^ (RowKey != null ? RowKey.GetHashCode() : 0);
            }
        }

        public static bool operator ==(OrderBookItemEntity left, OrderBookItemEntity right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OrderBookItemEntity left, OrderBookItemEntity right)
        {
            return !Equals(left, right);
        }
    }
}
