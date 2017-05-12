using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingBot.AlphaEngine
{
    public class PriceCurve
    {
        public PriceCurve(string instrument)
        {
            Instrument = instrument;
        }

        private List<IntrinsicTimeEvent> intrinsicTimeEvents = new List<IntrinsicTimeEvent>();

        public IReadOnlyList<IntrinsicTimeEvent> IntrinsicTimeEvents => intrinsicTimeEvents;

        public string Instrument { get; }
        
        private decimal basePrice;
        private bool directionUp;

        public void HandlePriceChange(decimal price, DateTime time)
        {
            if (basePrice == default(decimal))
            {
                basePrice = price;
                Console.WriteLine($"{Instrument}: Base price setted to {price}");
                return;
            }

            decimal percentChange = price / basePrice - 1;
            if (Math.Abs(percentChange) > AlphaEngineConfig.DirectionalChangeThreshold)
            {
                if (directionUp && percentChange > 0 || !directionUp && percentChange < 0)
                {
                    intrinsicTimeEvents.Add(new Overshoot(time));
                    Console.WriteLine($"{Instrument}: Overshoot event registered");
                }
                else if (directionUp && percentChange < 0 || !directionUp && percentChange > 0)
                {
                    intrinsicTimeEvents.Add(new DirectionalChange(time));
                    directionUp = !directionUp;

                    Console.WriteLine($"{Instrument}: DirectionalChange event registered");
                }

                basePrice = price;
            }
        }
    }
}
