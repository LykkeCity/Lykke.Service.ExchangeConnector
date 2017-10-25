using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssOrderOpen : GdaxWssMessageBase
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
