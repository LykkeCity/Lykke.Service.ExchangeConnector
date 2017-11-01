using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    internal sealed class InfoResponse : EventResponse
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
