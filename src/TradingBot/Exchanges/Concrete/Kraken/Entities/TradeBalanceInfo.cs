using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class TradeBalanceInfo
    {
        [JsonProperty("eb")]
        public string EquivalentBalance { get; set; }
        
        [JsonProperty("tb")]
        public string TradeBalance { get; set; }
        
        [JsonProperty("m")]
        public string MarginAmount { get; set; }
        
        [JsonProperty("n")]
        public string UnrealizedNetPnL { get; set; }
        
        [JsonProperty("c")]
        public string CostBasis { get; set; }
        
        [JsonProperty("v")]
        public string FloatingValuation { get; set; }
        
        /// <summary>
        /// Trade balance + unrealized net profit/loss
        /// </summary>
        [JsonProperty("e")]
        public string Equity { get; set; }
        
        [JsonProperty("mf")]
        public string FreeMargin { get; set; }
        
        [JsonProperty("ml")]
        public string MarginLevel { get; set; }
    }
}