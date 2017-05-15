using System.Collections.Generic;

namespace TradingBot.Exchanges.OandaApi.Entities.Instruments
{
    public class CandlesResponse
    {
        /// <summary>
        /// The instrument whose Prices are represented by the candlesticks.
        /// </summary>
        public string Instrument { get; set; }

        /// <summary>
        /// The granularity of the candlesticks provided.
        /// </summary>
        public CandlestickGranularity Granularity { get; set; }

        /// <summary>
        /// The list of candlesticks that satisfy the request.
        /// </summary>
        public List<Candlestick> Candles { get; set; }
    }
}
