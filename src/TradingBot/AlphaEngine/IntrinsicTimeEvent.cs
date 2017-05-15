using System;

namespace TradingBot.AlphaEngine
{
    public abstract class IntrinsicTimeEvent
    {
        public IntrinsicTimeEvent(DateTime time, AlgorithmMode mode, decimal priceMove)
        {
            Time = time;
            Mode = mode;
            PriceMove = priceMove;
        }

        public DateTime Time { get; }

        public AlgorithmMode Mode { get; }

        public decimal PriceMove { get; }
    }

    public class DirectionalChange : IntrinsicTimeEvent
    {
        public DirectionalChange(DateTime time, AlgorithmMode mode, decimal priceMove) 
            : base(time, mode, priceMove)
        {
        }
    }

    public class Overshoot : IntrinsicTimeEvent
    {
        public Overshoot(DateTime time, AlgorithmMode mode, decimal priceMove) 
            : base(time, mode, priceMove)
        {
        }
    }
}
