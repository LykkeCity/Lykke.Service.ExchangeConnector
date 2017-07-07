using System;
using Newtonsoft.Json;

namespace TradingBot.Common.Trading
{
    public class ExecutedTrade
    {
        [JsonConstructor]
        public ExecutedTrade(DateTime time, decimal price, decimal volume, TradeType type)
        {
            Time = time;
            Price = price;
            Volume = volume;
            Type = type;
        }
        
        public TradeType Type { get; }
        
        public DateTime Time { get; }
        
        public decimal Price { get; }
        
        public decimal Volume { get; }
    }
}