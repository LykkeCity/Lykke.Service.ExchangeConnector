using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
    public class BitfinexGetBase
    {
        [JsonProperty("request")]
        public string Request { get; set; }
    }
}
