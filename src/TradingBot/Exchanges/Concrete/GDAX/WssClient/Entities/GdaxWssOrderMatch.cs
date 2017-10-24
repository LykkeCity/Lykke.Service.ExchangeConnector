using System;
using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssOrderMatch : GdaxWssMessageBase
    {
        [JsonProperty("trade_id")]
        public decimal TradeId { get; set; }

        [JsonProperty("maker_order_id")]
        public string MakerOrderId { get; set; }

        [JsonProperty("taker_order_id")]
        public string TakerOrderId { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        public override string ToString()
        {
            return $"Match. TradeId: {TradeId}, Maker Order Id: {MakerOrderId}, " + 
                $"Taker Order Id: {TakerOrderId}, Time: {Time}, " + base.ToString();
        }
    }
}
