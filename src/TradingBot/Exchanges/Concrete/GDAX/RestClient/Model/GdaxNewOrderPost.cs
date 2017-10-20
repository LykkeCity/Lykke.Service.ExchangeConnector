using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxNewOrderPost : GdaxPostBase
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("side")]
        public GdaxOrderSide Side { get; set; }

        [JsonProperty("type")]
        public GdaxOrderType Type { get; set; }

        public override string ToString()
        {
            var text = $"ProductID: {ProductId}, Amount: {Size}, Price: {Price}, " + 
                $"Side: {Side}, Type: {Type}";
            return text;
        }
    }
}
