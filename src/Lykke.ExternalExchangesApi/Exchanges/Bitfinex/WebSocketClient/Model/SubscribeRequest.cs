using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{

    public sealed class SubscribeRequest
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("channel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public WsChannel Channel { get; set; }

        [JsonProperty("freq")]
        public string Freq { get; set; }

        [JsonProperty("prec")]
        public string Prec { get; set; }
    }
}
