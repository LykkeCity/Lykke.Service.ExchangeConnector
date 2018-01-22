using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public class GdaxSubscriptionChannel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<string> ProductIds { get; set; }
    }
}
