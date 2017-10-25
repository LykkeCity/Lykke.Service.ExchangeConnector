using System;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities
{
    internal class GdaxWssMessageBase
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("type")]
        public GdaxOrderType Type { get; set; }

        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("side")]
        public GdaxOrderSide Side { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return $"Type: {Type}, Product ID: {ProductId}, Time: {Time}, " + 
                $"Sequence: {Sequence}, Price: {Price}, Side: {Side}";
        }
    }
}
