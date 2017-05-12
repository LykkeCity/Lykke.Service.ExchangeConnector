using System;

namespace TradingBot.AlphaEngine
{
    public abstract class IntrinsicTimeEvent
    {
        public IntrinsicTimeEvent(DateTime time)
        {
            Time = time;
        }

        public DateTime Time { get; }
    }

    public class DirectionalChange : IntrinsicTimeEvent
    {
        public DirectionalChange(DateTime time) : base(time)
        {
        }
    }

    public class Overshoot : IntrinsicTimeEvent
    {
        public Overshoot(DateTime time) : base(time)
        {
        }
    }
}
