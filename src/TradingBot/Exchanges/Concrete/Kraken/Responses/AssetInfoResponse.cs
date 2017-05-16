using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.Kraken.Responses
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

    public enum AssetClass
    {
        Test,
        Unknown,
        Currency
    }
}
