using System;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    public abstract class IntrinsicTimeEvent
    {
        public IntrinsicTimeEvent(DateTime time, 
            AlgorithmMode mode, 
            decimal price, 
            decimal priceMove,
            decimal cascadingUnits)
        {
            Time = time;
            Mode = mode;
            Price = price;
            PriceMove = priceMove;
            CascadingUnits = cascadingUnits;
        }

        public DateTime Time { get; }

        public AlgorithmMode Mode { get; }

        public decimal PriceMove { get; }

        public decimal Price { get; }

        public decimal CascadingUnits { get; set; }
    }

    public class DirectionalChange : IntrinsicTimeEvent
    {
        public DirectionalChange(DateTime time, AlgorithmMode mode, 
            decimal price, decimal priceMove, decimal cascadingUnits) 
            : base(time, mode, price, priceMove, cascadingUnits)
        {
        }

        public override string ToString()
        {
            return $"Directional change to {Mode}";
        }
    }

    public class Overshoot : IntrinsicTimeEvent
    {
        public Overshoot(DateTime time, AlgorithmMode mode, decimal price, 
            decimal priceMove, decimal cascadingUnits)
            : base(time, mode, price, priceMove, cascadingUnits)
        {
        }

        public override string ToString()
        {
            return $"Overshoot to {Mode} for {PriceMove}";
        }
    }
}
