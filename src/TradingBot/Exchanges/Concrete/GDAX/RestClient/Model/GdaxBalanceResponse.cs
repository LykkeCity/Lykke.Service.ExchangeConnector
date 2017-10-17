using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxBalanceResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("available")]
        public decimal Available { get; set; }

       public override string ToString()
       {
          var str = string.Format("Type: {0}, Currency: {1}, Amount: {2}, Available: {3}", Type, Currency, Amount,Available);
          return str;
       }
    }

}
