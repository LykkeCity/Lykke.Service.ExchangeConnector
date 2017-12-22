using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class Attributes
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
