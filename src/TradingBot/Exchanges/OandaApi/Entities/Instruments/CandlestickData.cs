using Newtonsoft.Json;
using TradingBot.Exchanges.OandaApi.Entities.Prices;

namespace TradingBot.Exchanges.OandaApi.Entities.Instruments
{
    /// <summary>
    /// http://developer.oanda.com/rest-live-v20/instrument-df/#CandlestickData
    /// </summary>
    public class CandlestickData
    {
        /// <summary>
        /// The first (open) price in the time-range represented by the candlestick.
        /// </summary>
        [JsonProperty("o")]
        public decimal Open { get; set; }

        /// <summary>
        /// The highest price in the time-range represented by the candlestick.
        /// </summary>
        [JsonProperty("h")]
        public decimal Highest { get; set; }

        /// <summary>
        /// The lowest price in the time-range represented by the candlestick.
        /// </summary>
        [JsonProperty("l")]
        public decimal Lowest { get; set; }

        /// <summary>
        /// The last (closing) price in the time-range represented by the
        /// candlestick.
        /// </summary>
        [JsonProperty("c")]
        public decimal Closing { get; set; }
    }
}
