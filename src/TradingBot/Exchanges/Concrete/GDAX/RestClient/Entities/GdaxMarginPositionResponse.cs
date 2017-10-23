using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities
{
    internal sealed class GdaxMarginPositionResponse
   {
      [JsonProperty("id")]
      public int Id { get; set; }

      [JsonProperty("symbol")]
      public string Symbol { get; set; }

      [JsonProperty("status")]
      public string Status { get; set; }

      [JsonProperty("base")]
      public string Base { get; set; }

      [JsonProperty("amount")]
      public string Amount { get; set; }

      [JsonProperty("timestamp")]
      public string Timestamp { get; set; }

      [JsonProperty("swap")]
      public string Swap { get; set; }

      [JsonProperty("pl")]
      public string Pl { get; set; }

      public override string ToString()
      {
         var str = string.Format("Id: {0}, Symbol: {1}, Status: {2}, Base: {3}, Amount: {4}, Timestamp: {5}" +
                                 "Swap: {6}, Pl: {7}", Id, Symbol, Status, Base, Amount, Timestamp, Swap, Pl);
         return str;
      }
   }
}
