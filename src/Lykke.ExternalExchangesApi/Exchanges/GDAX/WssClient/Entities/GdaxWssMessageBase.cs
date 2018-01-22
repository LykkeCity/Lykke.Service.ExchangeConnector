using System;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient.Entities
{
    public class GdaxWssMessageBase
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

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
