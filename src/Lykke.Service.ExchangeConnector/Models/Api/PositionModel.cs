using Newtonsoft.Json;

namespace TradingBot.Models.Api
{
    public sealed class PositionModel
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("positionVolume")]
        public decimal PositionVolume { get; set; }

        [JsonProperty("maintMarginUsed")]
        public decimal MaintMarginUsed { get; set; }

        [JsonProperty("realisedPnL")]
        public decimal RealisedPnL { get; set; }

        [JsonProperty("unrealisedPnL")]
        public decimal UnrealisedPnL { get; set; }

        [JsonProperty("value")]
        public decimal? PositionValue { get; set; }

        [JsonProperty("availableMargin")]
        public decimal? AvailableMargin { get; set; }

        [JsonProperty("initialMarginRequirement")]
        public decimal InitialMarginRequirement { get; set; }

        [JsonProperty("maintenanceMarginRequirement")]
        public decimal MaintenanceMarginRequirement { get; set; }
    }
}
