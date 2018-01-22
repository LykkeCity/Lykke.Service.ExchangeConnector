using System;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient.Entities
{
    public class GdaxWssOrderReceived : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public Guid OrderId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }
        
        public override string ToString()
        {
            return $"Received. OrderID: {OrderId}, Size: {Size}, " + base.ToString();
        }
    }
}
