using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal class GdaxPostBase
    {
        [JsonProperty("request")]
        public string Request { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }
    }

}
