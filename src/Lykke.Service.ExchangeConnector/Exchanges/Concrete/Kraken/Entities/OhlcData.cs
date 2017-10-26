using System;
using System.Collections.Generic;
using TradingBot.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class OhlcDataResult
    {
        public Dictionary<string, IEnumerable<OhlcData>> Data { get; set; }

        public long Last { get; set; }
    }

    public class OhlcData
    {
        public OhlcData()
        {

        }

        public OhlcData(decimal[] values)
        {
            Time = DateTimeUtils.FromUnix((long)values[0]);
            Open = values[1];
            Hight = values[2];
            Low = values[3];
            Close = values[4];
            Vwap = values[5];
            Volume = values[6];
            Count = (int)values[7];
        }

        public DateTime Time { get; set; }
        
        public decimal Open { get; set; }

        public decimal Hight { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public decimal Vwap { get; set; }

        public decimal Volume { get; set; }

        public int Count { get; set; }
    }
}
