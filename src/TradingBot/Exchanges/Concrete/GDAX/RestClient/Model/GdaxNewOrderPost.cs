using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxNewOrderPost : GdaxPostBase
   {
      [JsonProperty("symbol")]
      public string Symbol { get; set; }

      [JsonProperty("amount")]
      public decimal Amount { get; set; }

      [JsonProperty("price")]
      public decimal Price { get; set; } 

      [JsonProperty("exchange")]
      public string Exchange { get; set; }

      [JsonProperty("side")]
      public string Side { get; set; }

      [JsonProperty("type")]
      public string Type { get; set; }

      [JsonProperty("ocoorder")]
      public string Ocoorder { get; set; }

      [JsonProperty("buy_price_oco")]
      public decimal BuyPriceOco { get; set; }

      [JsonProperty("sell_price_oco")]
      public decimal SellPriceOco { get; set; }

      public override string ToString()
      {
         var str = string.Format("Symbol: {0}, Amount: {1}, Price: {2}, Exchange: {3}, Side: {4}, Type: {5}", 
                                 Symbol,Amount,Price,Exchange,Side,Type);
         return str;
      }
   }
}
