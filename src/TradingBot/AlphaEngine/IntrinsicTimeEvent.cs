using System;

namespace TradingBot.AlphaEngine
{
    public abstract class IntrinsicTimeEvent
    {
        public IntrinsicTimeEvent(DateTime time, AlgorithmMode mode, decimal price, decimal priceMove)
        {
            Time = time;
            Mode = mode;
            Price = price;
            PriceMove = priceMove;
        }

        public DateTime Time { get; }

        public AlgorithmMode Mode { get; }

        public decimal PriceMove { get; }

        public decimal Price { get; }
    }

    public class DirectionalChange : IntrinsicTimeEvent
    {
        public DirectionalChange(DateTime time, AlgorithmMode mode, decimal price, decimal priceMove) 
            : base(time, mode, price, priceMove)
        {
        }

        public override string ToString()
        {
            return $"Directional change to {Mode}";
        }
    }

    public class Overshoot : IntrinsicTimeEvent
    {
        public Overshoot(DateTime time, AlgorithmMode mode, decimal price, decimal priceMove) 
            : base(time, mode, price, priceMove)
        {
        }

        public override string ToString()
        {
            return $"Overshoot to {Mode} for {PriceMove}";
        }
    }
}
