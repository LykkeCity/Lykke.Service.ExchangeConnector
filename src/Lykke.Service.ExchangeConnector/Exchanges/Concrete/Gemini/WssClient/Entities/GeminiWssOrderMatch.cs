using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities
{
    internal class GeminiWssOrderMatch : GeminiWssMessageBase
    {
        [JsonProperty("trade_id")]
        public decimal TradeId { get; set; }

        [JsonProperty("maker_order_id")]
        public Guid MakerOrderId { get; set; }

        [JsonProperty("taker_order_id")]
        public Guid TakerOrderId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        public override string ToString()
        {
            return $"Match. TradeId: {TradeId}, Maker Order Id: {MakerOrderId}, " + 
                $"Taker Order Id: {TakerOrderId}, Time: {Time}, " + base.ToString();
        }
    }
}
