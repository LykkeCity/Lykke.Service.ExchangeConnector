using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
{
    public class RowItem
    {
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Side? Side { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "bidSize")]
        public decimal? BidSize { get; set; }

        [JsonProperty(PropertyName = "bidPrice")]
        public decimal? BidPrice { get; set; }

        [JsonProperty(PropertyName = "askPrice")]
        public decimal? AskPrice { get; set; }

        [JsonProperty(PropertyName = "askSize")]
        public decimal? AskSize { get; set; }

        [JsonProperty(PropertyName = "orderID")]
        public string OrderID { get; set; }

        [JsonProperty(PropertyName = "clOrdID")]
        public string ClOrdID { get; set; }   
        
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }   
        
        [JsonProperty(PropertyName = "ordType")]
        public string OrdType { get; set; }

        [JsonProperty(PropertyName = "orderQty")]
        public decimal? OrderQty { get; set; }

        [JsonProperty(PropertyName = "cumQty")]
        public decimal? CumQty { get; set; }

        [JsonProperty(PropertyName = "ordStatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OrdStatus? OrdStatus { get; set; }      
        
        [JsonProperty(PropertyName = "execType")]
        public string ExecType { get; set; }

        [JsonProperty(PropertyName = "avgPx")]
        public decimal? AvgPx { get; set; }


    }
}
