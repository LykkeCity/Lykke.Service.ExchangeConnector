using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class AssetInfo
    {
        [JsonProperty("aclass")]
        public AssetClass AssetClass { get; set; }

        public string AltName { get; set; }

        public int Decimals { get; set; }

        [JsonProperty("display_decimals")]
        public int DisplayDecimals { get; set; }
    }
}
