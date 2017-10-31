using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal sealed class GeminiOrderResponse
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("side")]
        public GeminiOrderSide Side { get; set; }

        [JsonProperty("stp")]
        public string Stp { get; set; }

        [JsonProperty("type")]
        public GeminiOrderType OrderType { get; set; }

        [JsonProperty("time_in_force")]
        public string TimeInForce { get; set; }

        [JsonProperty("post_only")]
        public string PostOnly { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("fill_fees")]
        public decimal FillFees { get; set; }

        [JsonProperty("filled_size")]
        public decimal FilledSize { get; set; }

        [JsonProperty("executed_value")]
        public decimal ExecutedValue { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("settled")]
        public bool Settled { get; set; }
    }
}
