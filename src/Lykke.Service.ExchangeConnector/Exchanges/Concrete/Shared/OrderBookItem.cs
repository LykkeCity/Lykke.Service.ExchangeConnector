using System;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class OrderBookItem : IEquatable<OrderBookItem>
    {
        private readonly Func<OrderBookItem, OrderBookItem, bool> _equalFunc;
        private readonly Func<OrderBookItem, int> _hashCodeFunc;

        public OrderBookItem(Func<OrderBookItem, OrderBookItem, bool> equalFunc, Func<OrderBookItem, int> hashCodeFunc)
        {
            _equalFunc = equalFunc;
            _hashCodeFunc = hashCodeFunc;
        }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public long Id { get; set; }

        public bool IsBuy { get; set; }

        public string Symbol { get; set; }

        public bool Equals(OrderBookItem other)
        {
            return _equalFunc(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderBookItem)obj);
        }

        public override int GetHashCode()
        {
            return _hashCodeFunc(this);
        }
    }
}
