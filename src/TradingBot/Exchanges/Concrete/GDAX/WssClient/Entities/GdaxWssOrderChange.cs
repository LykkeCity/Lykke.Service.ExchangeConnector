using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssOrderChange : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("new_size")]
        public decimal NewSize { get; set; }

        [JsonProperty("old_size")]
        public decimal OldSize { get; set; }

        public override string ToString()
        {
            return $"Change. OrderID: {OrderId}, Time: {Time}, New Size: {NewSize}, " + 
                $"Old Size: {OldSize}, " + base.ToString();
        }
    }

}
