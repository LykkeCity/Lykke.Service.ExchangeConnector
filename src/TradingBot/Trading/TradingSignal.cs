namespace TradingBot.Trading
{
    public enum SignalType
    {
        Long,
        Short
    }

    public class TradingSignal
    {
        public TradingSignal(SignalType type, decimal price, decimal count)
        {
            Type = type;
            Price = price;
            Count = count;
        }

        public SignalType Type { get; }

        public decimal Price { get; }

        public decimal Count { get; }

        public decimal Amount => Price * Count;
    }
}
