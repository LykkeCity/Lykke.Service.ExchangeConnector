using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public abstract class SubscribeChannelRequest : SubscribeRequest
    {
        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("channel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public WsChannel Channel { get; set; }

    }
}
