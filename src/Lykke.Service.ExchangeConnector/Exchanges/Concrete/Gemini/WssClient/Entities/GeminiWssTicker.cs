using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities
{
    internal class GeminiWssTicker : GeminiWssMessageBase
    {
        [JsonProperty("trade_id")]
        public long TradeId { get; set; }

        [JsonProperty("last_size")]
        public decimal LastSize { get; set; }

        [JsonProperty("best_bid")]
        public decimal BestBid { get; set; }

        [JsonProperty("best_ask")]
        public decimal BestAsk { get; set; }

        public override string ToString()
        {
            return $"Match. TradeId: {TradeId}, Product Id: {ProductId}, Last Size: {LastSize}, " + 
                $"Best Bid: {BestBid}, Best Ask: {BestAsk} Time: {Time}, " + base.ToString();
        }
    }
}
