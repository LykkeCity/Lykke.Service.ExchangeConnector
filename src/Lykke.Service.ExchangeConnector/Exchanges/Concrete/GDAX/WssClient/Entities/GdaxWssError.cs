using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssError
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
