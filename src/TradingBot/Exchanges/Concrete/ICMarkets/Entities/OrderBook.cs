using System;

namespace TradingBot.Exchanges.Concrete.ICMarkets.Entities
{
    public class OrderBook
    {
        public string Source { get; set; }
        
        public string Asset { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public PriceVolume[] Asks { get; set; }
        
        public PriceVolume[] Bids { get; set; }
    }
}