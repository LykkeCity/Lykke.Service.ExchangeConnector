﻿using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model
{
    internal sealed class MarginLimit
    {
        [JsonProperty("on_pair")]
        public string OnPair { get; set; }

        [JsonProperty("initial_margin")]
        public decimal InitialMargin { get; set; }

        [JsonProperty("margin_requirement")]
        public decimal MarginRequirement { get; set; }

        [JsonProperty("tradable_balance")]
        public decimal TradableBalance { get; set; }

        public override string ToString()
        {
            return $"OnPair:{OnPair} InitialMargin:{InitialMargin} MarginRequirement:{MarginRequirement} TradableBalance:{TradableBalance}";
        }
    }
}
