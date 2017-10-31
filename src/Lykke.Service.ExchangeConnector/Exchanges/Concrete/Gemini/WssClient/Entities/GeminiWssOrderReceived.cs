using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities
{
    internal class GeminiWssOrderReceived : GeminiWssMessageBase
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
