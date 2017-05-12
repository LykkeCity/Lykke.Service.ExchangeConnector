using System;

namespace TradingBot.OandaApi.Entities.Instruments
{
    /// <summary>
    /// http://developer.oanda.com/rest-live-v20/instrument-df/#Candlestick
    /// </summary>
    public class Candlestick
    {
        /// <summary>
        /// The start time of the candlestick
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The candlestick data based on bids. Only provided if bid-based candles
        /// were requested.
        /// </summary>
        public CandlestickData Bid { get; set; }
        
        /// <summary>
        /// The candlestick data based on asks. Only provided if ask-based candles
        /// were requested.
        /// </summary>
        public CandlestickData Ask { get; set; }

        /// <summary>
        /// The candlestick data based on midpoints. Only provided if midpoint-based
        /// candles were requested.
        /// </summary>
        public CandlestickData Mid { get; set; }

        /// <summary>
        /// The number of prices created during the time-range represented by the
        /// candlestick.
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// A flag indicating if the candlestick is complete. A complete candlestick
        /// is one whose ending time is not in the future.
        /// </summary>
        public bool Complete { get; set; }
    }
}
