using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public class EventMessageResponse : EventResponse
    {

        [JsonProperty("code")]
        public Code Code { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
