using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
{
    public class Filter
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
