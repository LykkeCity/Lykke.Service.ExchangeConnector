using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Exchanges.Concrete.Shared;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class RowItem
    {
        private static readonly Func<OrderBookItem, OrderBookItem, bool> EqualsFunc = Equals;
        private static readonly Func<OrderBookItem, int> GetHashCodeFunc = GetHashCode;


        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        public OrderBookItem ToOrderBookItem()
        {
            return new OrderBookItem(EqualsFunc, GetHashCodeFunc)
            {
                Id = Id.ToString(CultureInfo.InvariantCulture),
                IsBuy = Side == Side.Buy,
                Price = Price,
                Symbol = Symbol,
                Size = Size
            };
        }


        private static bool Equals(OrderBookItem @this, OrderBookItem other)
        {
            return @this.Id == other.Id && @this.IsBuy == other.IsBuy && string.Equals(@this.Symbol, other.Symbol);
        }

        private static int GetHashCode(OrderBookItem @this)
        {
            unchecked
            {
                var hashCode = @this.Id.GetHashCode();
                hashCode = (hashCode * 397) ^ @this.IsBuy.GetHashCode();
                hashCode = (hashCode * 397) ^ (@this.Symbol != null ? @this.Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
