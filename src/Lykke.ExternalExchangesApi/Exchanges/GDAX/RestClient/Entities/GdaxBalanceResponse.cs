using System;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public sealed class GdaxBalanceResponse
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("profile_id")]
        public Guid ProfileId { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        [JsonProperty("hold")]
        public decimal Hold { get; set; }

        [JsonProperty("available")]
        public decimal Available { get; set; }

        [JsonProperty("margin_enabled")]
        public bool MarginEnabled { get; set; }

        [JsonProperty("funded_amount")]
        public decimal FundedAmount { get; set; }

        [JsonProperty("default_amount")]
        public decimal DefaultAmount { get; set; }
    }

}
