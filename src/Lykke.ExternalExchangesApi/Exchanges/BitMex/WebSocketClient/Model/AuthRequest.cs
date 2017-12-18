using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
{
    public class AuthRequest
    {
        [JsonProperty("op")]
        public string Operation { get; set; }

        [JsonProperty("args")]
        public IReadOnlyCollection<object> Arguments { get; set; }
    }
}
