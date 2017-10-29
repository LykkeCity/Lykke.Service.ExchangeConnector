using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class AssetPair
    {
        public string AltName { get; set; }

        public string Base { get; set; }

        [JsonProperty("aclass_base")]
        public AssetClass BaseAssetClass { get; set; }

        public string Quote { get; set; }

        [JsonProperty("aclass_quote")]
        public AssetClass QuoteAssetClass { get; set; }

        public LotType Lot { get; set; }

        [JsonProperty("pair_decimals")]
        public int PairDecimals { get; set; }

        [JsonProperty("lot_decimals")]
        public int LotDecimals { get; set; }

        [JsonProperty("lot_multiplier")]
        public int LotMultiplier { get; set; }

        [JsonProperty("leverage_buy")]
        public int[] LeverageBuy { get; set; }

        [JsonProperty("leverage_sell")]
        public int[] LeverageSell { get; set; }

        public decimal[][] Fees { get; set; }
        
        [JsonProperty("fees_maker")]
        public decimal[][] FeesMaker { get; set; }

        public IEnumerable<Tuple<int, decimal>> FeesInTuples =>
            FeesArrayToTuples(Fees);
        
        public IEnumerable<Tuple<int, decimal>> FeesMakerInTuples =>
            FeesArrayToTuples(FeesMaker);

        private IEnumerable<Tuple<int, decimal>> FeesArrayToTuples(decimal[][] fees)
            => fees?.Select(x => Tuple.Create((int)x[0], x[1]));

        [JsonProperty("fee_volume_currency")]
        public string FeeVolumeCurrency { get; set; }

        [JsonProperty("margin_call")]
        public int MarginCall { get; set; }

        [JsonProperty("margin_stop")]
        public int MarginStop { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, Base={1}, Quote={2}", AltName, Base, Quote);
        }
    }
}
