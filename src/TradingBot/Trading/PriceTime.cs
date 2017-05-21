using System;

namespace TradingBot.Trading
{
    public class PriceTime
    {
        public PriceTime()
        {

        }

        public PriceTime(decimal price, DateTime time)
        {
            Price = price;
            Time = time;
        }

        public DateTime Time { get; set; }

        public decimal Price { get; set; }
    }
}
