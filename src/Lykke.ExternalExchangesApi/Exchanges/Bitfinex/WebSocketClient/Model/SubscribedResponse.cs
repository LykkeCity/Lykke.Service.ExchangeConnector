using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class SubscribedResponse : EventResponse
    {
        [JsonProperty("freq")]
        public string Freq { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("chanId")]
        public long ChanId { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("len")]
        public string Len { get; set; }

        [JsonProperty("prec")]
        public string Prec { get; set; }
    }
}
