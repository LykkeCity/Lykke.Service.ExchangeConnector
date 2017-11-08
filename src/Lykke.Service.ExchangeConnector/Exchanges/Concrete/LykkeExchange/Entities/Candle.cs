using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class Candle
    {
        [JsonProperty(PropertyName = "a")]
        public string Asset { get; set; }

        [JsonProperty(PropertyName = "p")]
        public string AskBid { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string Interval { get; set; }

        [JsonProperty(PropertyName = "t")]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "o")]
        public decimal O { get; set; }

        [JsonProperty(PropertyName = "c")]
        public decimal C { get; set; }

        [JsonProperty(PropertyName = "h")]
        public decimal H { get; set; }

        [JsonProperty(PropertyName = "l")]
        public decimal L { get; set; }
    }
}
