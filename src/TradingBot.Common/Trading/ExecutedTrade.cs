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
            Fee = 0; // TODO
        }
        
        public TradeType Type { get; }
        
        public DateTime Time { get; }
        
        public decimal Price { get; }
        
        public decimal Volume { get; }
        
        public decimal Fee { get; }
        
        // TODO: link to order (ID, probaly)

        public override string ToString()
        {
            return $"{Type} at {Time} for {Price} times {Volume}";
        }
    }
}