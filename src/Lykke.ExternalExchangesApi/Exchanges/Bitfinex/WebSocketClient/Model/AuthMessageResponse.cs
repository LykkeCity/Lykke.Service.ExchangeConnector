using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class AuthMessageResponse : EventMessageResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("userId")]
        public long UserId { get; set; }
    }
}