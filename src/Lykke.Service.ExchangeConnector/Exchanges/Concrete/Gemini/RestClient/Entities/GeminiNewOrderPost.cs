using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal sealed class GeminiNewOrderPost : GeminiPostContentBase
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("side")]
        public GeminiOrderSide Side { get; set; }

        [JsonProperty("type")]
        public GeminiOrderType Type { get; set; }

        public override string ToString()
        {
            var text = $"ProductID: {ProductId}, Amount: {Size}, Price: {Price}, " + 
                $"Side: {Side}, Type: {Type}";
            return text;
        }
    }
}
