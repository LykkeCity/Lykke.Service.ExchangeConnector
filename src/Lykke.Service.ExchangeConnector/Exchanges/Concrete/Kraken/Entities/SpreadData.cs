using System;
using System.Collections.Generic;
using Lykke.ExternalExchangesApi.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class SpreadDataResult
    {
        public Dictionary<string, IEnumerable<SpreadData>> Data { get; set; }

        public long Last { get; set; }
    }

    public class SpreadData
    {
        public SpreadData()
        {
            
        }

        public SpreadData(decimal[] values)
        {
			Time = DateTimeUtils.FromUnix((long)values[0]);
			Bid = values[1];
			Ask = values[2];
        }

        public DateTime Time { get; }

        public decimal Bid { get; }

        public decimal Ask { get; }
}
}
