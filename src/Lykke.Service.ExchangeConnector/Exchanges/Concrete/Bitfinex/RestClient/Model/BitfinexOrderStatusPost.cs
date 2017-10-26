using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model
{
    internal sealed class BitfinexOrderStatusPost : BitfinexPostBase
   {
      /// <summary>
      /// This class can be used to send a cancel message in addition to 
      /// retrieving the current status of an order.
      /// </summary>
      [JsonProperty("order_id")]
      public long OrderId { get; set; }
   }
}
