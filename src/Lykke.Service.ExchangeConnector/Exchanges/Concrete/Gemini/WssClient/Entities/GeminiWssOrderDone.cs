using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities
{
    internal class GeminiWssOrderDone : GeminiWssMessageBase
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
