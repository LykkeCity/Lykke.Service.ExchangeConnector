using System;
using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    public class OrderBookItemResponse
    {
        private static readonly Func<OrderBookItem, OrderBookItem, bool> EqualsFunc = Equals;
        private static readonly Func<OrderBookItem, int> GetHashCodeFunc = GetHashCode;

        public long Id { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        public string Pair { get; set; }

        public OrderBookItem ToOrderBookItem()
        {
            return new OrderBookItem(EqualsFunc, GetHashCodeFunc)
            {
                Id = Id,
                IsBuy = Amount > 0,
                Price = Price,
                Symbol = Pair,
                Size = Amount
            };
        }


        private static bool Equals(OrderBookItem @this, OrderBookItem other)
        {
            return @this.Id == other.Id;
        }

        private static int GetHashCode(OrderBookItem @this)
        {
            var hashCode = @this.Id.GetHashCode();
            return hashCode;
        }
    }
}
