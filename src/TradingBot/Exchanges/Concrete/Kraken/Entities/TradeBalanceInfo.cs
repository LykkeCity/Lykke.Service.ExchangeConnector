using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public sealed class TradeBalanceInfo
    {
        [JsonProperty("equivalentBalance")]
        public double? EquivalentBalance { get; set; }
        
        [JsonProperty("tradeBalance")]
        public double? TradeBalance { get; set; }
        
        [JsonProperty("marginAmount")]
        public double? MarginAmount { get; set; }
        
        [JsonProperty("unrealizedNetPnL")]
        public double? UnrealizedNetPnL { get; set; }
        
        [JsonProperty("costBasis")]
        public double? CostBasis { get; set; }
        
        [JsonProperty("floatingValuation")]
        public double? FloatingValuation { get; set; }
        
        /// <summary>
        /// Trade balance + unrealized net profit/loss
        /// </summary>
        [JsonProperty("equity")]
        public double? Equity { get; set; }
        
        [JsonProperty("freeMargin")]
        public double? FreeMargin { get; set; }
        
        [JsonProperty("marginLevel")]
        public double? MarginLevel { get; set; }
    }
}
