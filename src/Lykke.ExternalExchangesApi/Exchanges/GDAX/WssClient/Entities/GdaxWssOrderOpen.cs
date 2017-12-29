using System;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient.Entities
{
    public class GdaxWssOrderOpen : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public Guid OrderId { get; set; }

        [JsonProperty("remaining_size")]
        public decimal RemainingSize { get; set; }

        public override string ToString()
        {
            return $"Open. OrderID: {OrderId}, Remaining Size: {RemainingSize}, " + base.ToString();
        }
    }
}
