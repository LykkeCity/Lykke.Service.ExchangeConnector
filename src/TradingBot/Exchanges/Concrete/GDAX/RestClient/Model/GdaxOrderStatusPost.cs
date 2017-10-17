using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxOrderStatusPost : GdaxPostBase
   {
      /// <summary>
      /// This class can be used to send a cancel message in addition to 
      /// retrieving the current status of an order.
      /// </summary>
      [JsonProperty("order_id")]
      public long OrderId { get; set; }
   }
}
