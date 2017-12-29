using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
{
    public class Attributes
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
