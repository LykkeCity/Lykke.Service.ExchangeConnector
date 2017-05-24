using System;

namespace TradingBot.Trading
{
    public enum SignalType
    {
        Long,
        Short
    }

    public class TradingSignal
    {
        public TradingSignal(SignalType type, decimal price, decimal count, DateTime time)
        {
            Type = type;
            Price = price;
            Count = count;
            Time = time;
        }

        public DateTime Time { get; }

        public SignalType Type { get; }

        public decimal Price { get; }

        public decimal Count { get; }

        public decimal Amount => Price * Count;
    }
}
