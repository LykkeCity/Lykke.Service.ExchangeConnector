using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class RowItem
    {
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

        protected bool Equals(RowItem other)
        {
            return Id == other.Id && Side == other.Side && string.Equals(Symbol, other.Symbol);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RowItem)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Side;
                hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
