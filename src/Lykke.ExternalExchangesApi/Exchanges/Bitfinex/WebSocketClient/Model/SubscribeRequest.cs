using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public abstract class SubscribeRequest
    {
        [JsonProperty("event")]
        public string Event { get; set; }
    }
}