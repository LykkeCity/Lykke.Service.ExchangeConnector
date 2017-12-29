using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public sealed class GdaxOrderBookRawResponse
    {
        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("bids")]
        public ICollection<string[]> Bids { get; set; }

        [JsonProperty("asks")]
        public ICollection<string[]> Asks { get; set; }
    }
}
