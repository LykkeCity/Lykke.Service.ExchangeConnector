using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssOrderReceived : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }
        
        public override string ToString()
        {
            return $"Received. OrderID: {OrderId}, Size: {Size}, " + base.ToString();
        }
    }
}
