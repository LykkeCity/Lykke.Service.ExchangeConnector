using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities
{
    internal class GeminiWssOrderChange : GeminiWssMessageBase
    {
        [JsonProperty("order_id")]
        public Guid OrderId { get; set; }

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
