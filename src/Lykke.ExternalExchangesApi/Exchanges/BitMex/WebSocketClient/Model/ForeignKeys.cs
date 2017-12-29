using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
{
    public class ForeignKeys
    {
        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
