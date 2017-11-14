using System;

namespace TradingBot.Exchanges.Concrete.Shared
{
    public class OrderBookItem : IEquatable<OrderBookItem>
    {
        private readonly Func<OrderBookItem, OrderBookItem, bool> _equalFunc;
        private readonly Func<OrderBookItem, int> _hashCodeFunc;

        public OrderBookItem()
        {
            _equalFunc = StandardEquals;
            _hashCodeFunc = StandardGetHashCode;
        }

        public OrderBookItem(Func<OrderBookItem, OrderBookItem, bool> equalFunc, Func<OrderBookItem, int> hashCodeFunc)
        {
            _equalFunc = equalFunc;
            _hashCodeFunc = hashCodeFunc;
        }

        public decimal Price { get; set; }

        public decimal Size { get; set; }

        public string Id { get; set; }

        public bool IsBuy { get; set; }

        public string Symbol { get; set; }

        private static bool StandardEquals(OrderBookItem @this, OrderBookItem other)
        {
            return @this.Id == other.Id && @this.IsBuy == other.IsBuy && string.Equals(@this.Symbol, other.Symbol);
        }

        private static int StandardGetHashCode(OrderBookItem @this)
        {
            unchecked
            {
                var hashCode = @this.Id.GetHashCode();
                hashCode = (hashCode * 397) ^ @this.IsBuy.GetHashCode();
                hashCode = (hashCode * 397) ^ (@this.Symbol != null ? @this.Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
        
        public bool Equals(OrderBookItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
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
