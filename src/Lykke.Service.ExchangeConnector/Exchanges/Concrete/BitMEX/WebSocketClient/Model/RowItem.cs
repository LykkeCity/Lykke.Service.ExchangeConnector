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
        public decimal? Price { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Side Side { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "bidSize")]
        public double? BidSize { get; set; }

        [JsonProperty(PropertyName = "bidPrice")]
        public double? BidPrice { get; set; }

        [JsonProperty(PropertyName = "askPrice")]
        public double? AskPrice { get; set; }

        [JsonProperty(PropertyName = "askSize")]
        public double? AskSize { get; set; }

        [JsonProperty(PropertyName = "orderID")]
        public string OrderID { get; set; }

        [JsonProperty(PropertyName = "clOrdID")]
        public string ClOrdID { get; set; }

        [JsonProperty(PropertyName = "orderQty")]
        public double? OrderQty { get; set; }

        [JsonProperty(PropertyName = "cumQty")]
        public double? CumQty { get; set; }

        [JsonProperty(PropertyName = "ordStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrdStatus OrdStatus { get; set; }

        [JsonProperty(PropertyName = "avgPx")]
        public decimal? AvgPx { get; set; }

        public OrderBookItem ToOrderBookItem()
        {
            return new OrderBookItem(EqualsFunc, GetHashCodeFunc)
            {
                Id = Id.ToString(CultureInfo.InvariantCulture),
                IsBuy = Side == Side.Buy,
                Price = Price ?? 0,
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
