using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class AuthRequest
    {
        [JsonProperty("op")]
        public string Operation { get; set; }

        [JsonProperty("args")]
        public IReadOnlyCollection<object> Arguments { get; set; }
    }
}
