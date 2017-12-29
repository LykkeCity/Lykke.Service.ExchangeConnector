using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
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
