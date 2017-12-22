using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class ErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }

        public const string Token = "error";
    }
}
