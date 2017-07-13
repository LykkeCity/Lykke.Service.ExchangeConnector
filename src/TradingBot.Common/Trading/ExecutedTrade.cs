using System;
using Newtonsoft.Json;

namespace TradingBot.Common.Trading
{
    public class ExecutedTrade
    {
        [JsonConstructor]
        public ExecutedTrade(DateTime time, decimal price, decimal volume, TradeType type, long orderId)
        {
            Time = time;
            Price = price;
            Volume = volume;
            Type = type;
            Fee = 0; // TODO
            OrderId = orderId;
        }
        
        public TradeType Type { get; }
        
        public DateTime Time { get; }
        
        public decimal Price { get; }
        
        public decimal Volume { get; }
        
        public decimal Fee { get; }
        
        public long OrderId { get; }

        public override string ToString()
        {
            return $"OrderId: {OrderId}. {Type} at {Time}. Price: {Price}. Volume: {Volume}";
        }
    }
}