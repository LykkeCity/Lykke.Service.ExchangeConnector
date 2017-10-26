using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public sealed class SuccessResponse
    {
        [JsonProperty("subscribe")]
        public string Subscribe { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        public const string Token = "success";
    }
}
