using System;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient.Entities
{
    public class GdaxWssOrderDone : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public Guid OrderId { get; set; }

        [JsonProperty("remaining_size")]
        public decimal RemainingSize { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        public override string ToString()
        {
            return $"Done. OrderID: {OrderId}, Remaining Size: {RemainingSize}, " + 
                $"Reason: {Reason}, " + base.ToString();
        }
    }
}
