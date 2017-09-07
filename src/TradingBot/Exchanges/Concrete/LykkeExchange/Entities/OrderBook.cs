using System;
using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class OrderBook
    {
        public string AssetPair { get; set; }
        
        public bool IsBuy { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public List<PriceVolume> Prices { get; set; }
    }
    
    public class PriceVolume
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }
}