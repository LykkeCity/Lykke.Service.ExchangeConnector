using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{

    public sealed class SubscribeRequest
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("pair")]
        public string Pair { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("freq")]
        public string Freq { get; set; }

        [JsonProperty("prec")]
        public string Prec { get; set; }
    }
}
