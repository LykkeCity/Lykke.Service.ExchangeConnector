using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
    public class BitfinexPostBase
    {
        [JsonProperty("request")]
        public string Request { get; set; }

        [JsonProperty("nonce")]
        public string Nonce { get; set; }
    }

}
