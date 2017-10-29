using Newtonsoft.Json;
using System;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class Ticker
    {
        public decimal[] a { get; set; }

        public TradeInfo Ask => new TradeInfo(a);

        public decimal[] b { get; set; }

        public TradeInfo Bid => new TradeInfo(b);

        /// <summary>
        /// Today's opening price
        /// </summary>
        [JsonProperty("o")]
        public decimal Open { get; set; }


        public decimal[] h { get; set; }

        public decimal HightToday => h[0];

        public decimal Hight24Hours => h[1];


        public decimal[] l { get; set; }

        public decimal LowToday => l[0];

        public decimal Low24Hours => l[1];


        public int[] t { get; set; }

        public int TradesToday => t[0];

        public int Trades24Hours => t[1];

        
        public decimal[] v { get; set; }

        public decimal VolumeToday => v[0];

        public decimal Volume24Hours => v[1];


        public decimal[] p { get; set; }

        public decimal VolumeWeightedAverageToday => p[0];

        public decimal VolumeWeightedAverage24Hours => p[1];


        public decimal[] c { get; set; }

        public decimal LastTradePrice => c[0];

        public decimal LastTradeVolume => c[1];
    }

    public class TradeInfo
    {
        public TradeInfo(decimal[] values)
        {
            if (values.Length != 3)
                throw new ArgumentException(nameof(values));

            Price = values[0];
            WholeLotVolume = (int)values[1];
            LotVolume = values[2];
        }

        public TradeInfo(decimal price, int wholeLotVolume, decimal lotVolume)
        {
            Price = price;
            WholeLotVolume = wholeLotVolume;
            LotVolume = lotVolume;
        }

        public decimal Price { get; }

        public int WholeLotVolume { get; }

        public decimal LotVolume { get; }
    }

}
