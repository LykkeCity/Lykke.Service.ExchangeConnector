using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
    public sealed class Error
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

}
