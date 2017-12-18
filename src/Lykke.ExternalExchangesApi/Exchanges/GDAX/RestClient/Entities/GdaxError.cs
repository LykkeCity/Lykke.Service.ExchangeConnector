using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public sealed class GdaxError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

}
