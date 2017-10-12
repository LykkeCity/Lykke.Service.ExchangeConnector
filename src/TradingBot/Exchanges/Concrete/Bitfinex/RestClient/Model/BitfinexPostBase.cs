using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model
{
    internal class BitfinexPostBase
    {
        [JsonProperty("request")]
        public string Request { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }
    }

}
