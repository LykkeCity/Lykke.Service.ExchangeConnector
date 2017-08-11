using System;

namespace TradingBot.Trading
{
    public class OrderBook
    {
        public OrderBook(Instrument instrument, PriceVolume[] asks, PriceVolume[] bids)
        {
            Instrument = instrument;
            Asks = asks;
            Bids = bids;
        }

        public Instrument Instrument { get; }
        
        public PriceVolume[] Asks { get; }
        
        public PriceVolume[] Bids { get; }
    }

    public class PriceVolume
    {
        public PriceVolume(DateTime time, decimal price, decimal volume)
        {
            Time = time;
            Price = price;
            Volume = volume;
        }
        
        public decimal Price { get; }
        
        public decimal Volume { get; }
        
        public DateTime Time { get; }
    }
}