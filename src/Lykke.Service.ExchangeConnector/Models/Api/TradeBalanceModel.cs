using Newtonsoft.Json;

namespace TradingBot.Models.Api
{
    public sealed class TradeBalanceModel
    {
        [JsonProperty("accountCurrency")]
        public string AccountCurrency { get; set; }

        [JsonProperty("totalbalance")]
        public decimal Totalbalance { get; set; }

        [JsonProperty("unrealisedPnL")]
        public decimal UnrealisedPnL { get; set; }

        [JsonProperty("maringAvailable")]
        public decimal MaringAvailable { get; set; }

        [JsonProperty("marginUsed")]
        public decimal MarginUsed { get; set; }
    }
}
