using System;

namespace TradingBot.Common.Trading
{
    public enum SignalType
    {
        Long,
        Short
    }

    public enum OrderType
    {
        Market,
        Limit
    }

    public class TradingSignal
    {
        public TradingSignal(SignalType type, decimal price, decimal count, DateTime time, 
            OrderType orderType = OrderType.Market)
        {
            Type = type;
            Price = price;
            Count = count;
            Time = time;
            OrderType = orderType;
        }

        public DateTime Time { get; }

        public SignalType Type { get; }
        
        public OrderType OrderType { get; }

        public decimal Price { get; }

        public decimal Count { get; }

        public decimal Amount => Price * Count;
    }
}
