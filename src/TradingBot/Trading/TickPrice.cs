using System;

namespace TradingBot.Trading
{
    public class TickPrice
    {
        public TickPrice(DateTime time, decimal mid)
        {
            Time = time;
            Ask = mid;
            Bid = mid;
            Mid = mid;
        }

        public TickPrice(DateTime time, decimal ask, decimal bid)
        {
            Time = time;
            Ask = ask;
            Bid = bid;
            Mid = (ask + bid) / 2m;
        }

        public TickPrice(DateTime time, decimal ask, decimal bid, decimal mid)
        {
            Time = time;
            Ask = ask;
            Bid = bid;
            Mid = mid;
        }

        public DateTime Time { get; }

        public decimal Ask { get; }

        public decimal Bid { get; }

        public decimal Mid { get; }
    }
}
