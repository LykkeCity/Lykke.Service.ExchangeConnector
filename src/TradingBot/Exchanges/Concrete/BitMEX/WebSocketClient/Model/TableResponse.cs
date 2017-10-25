using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public class TableResponse
    {
        [JsonProperty("filter")]
        public Filter Filter { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Action Action { get; set; }

        [JsonProperty("data")]
        public RowItem[] Data { get; set; }

        [JsonProperty("keys")]
        public string[] Keys { get; set; }

        [JsonProperty("foreignKeys")]
        public ForeignKeys ForeignKeys { get; set; }

        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("types")]
        public ColumnTypes Types { get; set; }

        public const string Token = "table";

    }
}
