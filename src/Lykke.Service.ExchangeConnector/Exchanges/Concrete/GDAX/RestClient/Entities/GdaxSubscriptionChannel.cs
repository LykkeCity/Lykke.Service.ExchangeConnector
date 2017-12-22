using System.Collections.Generic;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities
{
    internal class GdaxSubscriptionChannel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<string> ProductIds { get; set; }
    }
}
