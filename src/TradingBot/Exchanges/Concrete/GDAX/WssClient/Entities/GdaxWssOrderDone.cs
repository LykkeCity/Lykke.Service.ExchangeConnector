using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssOrderDone : GdaxWssMessageBase
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

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
