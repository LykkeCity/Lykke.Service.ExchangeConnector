using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class InfoResponse : EventResponse
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
