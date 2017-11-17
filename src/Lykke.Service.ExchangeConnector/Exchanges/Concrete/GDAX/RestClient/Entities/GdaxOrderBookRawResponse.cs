using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities
{
    internal sealed class GdaxOrderBookRawResponse
    {
        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("bids")]
        public ICollection<string[]> Bids { get; set; }

        [JsonProperty("asks")]
        public ICollection<string[]> Asks { get; set; }
    }
}
