using Newtonsoft.Json;

namespace TradingBot.Models.Api
{
    public class ExchangeRatingModel
    {
        [JsonProperty("exchangeName")]
        public string ExchangeName { get; set; }

        [JsonProperty("rating")]
        public double Rating { get; set; }
    }
}
