using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class Filter
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
