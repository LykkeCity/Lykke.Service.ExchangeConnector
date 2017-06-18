using System;
namespace TradingBot.FixConnector
{
    public class PriceVolume
    {
        public decimal Price { get; }
        public decimal Volume { get; }

        public PriceVolume(decimal price, decimal volume)
        {
            Volume = volume;
            Price = price;
        }
    }
}
