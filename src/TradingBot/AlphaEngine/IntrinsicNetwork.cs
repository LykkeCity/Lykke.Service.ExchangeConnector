using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Common.Trading;
using TradingBot.Trading;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// Represents the set of IntrinsicTimes with different thresholds.
    /// see Multi-scale Representation of High Frequency Market Liquidity
    /// </summary>
    public class IntrinsicNetwork
    {
        public IntrinsicNetwork(int dimension, decimal firstThreshold, 
            TimeSpan liquiditySlidingWindow)
        {
            if (dimension < 1)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be greater then zero");
            
            var thresholds = new decimal[dimension];

            thresholds[0] = firstThreshold;

            for (int i = 1; i < dimension; i++)
            {
                thresholds[i] = thresholds[i - 1] * 2;
            }

            this.thresholds = thresholds;
            this.dimension = dimension;
            this.liquiditySlidingWindow = liquiditySlidingWindow;
            
            intrinsicTimes = thresholds.Select(x => new IntrinsicTime(x)).ToList();
        }

        private int dimension;

        public int Dimension => dimension;

        
        private decimal[] thresholds;
        private List<IntrinsicTime> intrinsicTimes;

        private List<Surprise> surprises = new List<Surprise>();
        private List<Liquidity> liquidities = new List<Liquidity>();

        public IReadOnlyList<Liquidity> Liquidities => liquidities;


        private TimeSpan liquiditySlidingWindow;
        private DateTime lastLiquidityCalcTime;
        private DateTime firstDayAfterWeekend;
        private NetworkState previousState;
        private TimeSpan weekend = TimeSpan.FromDays(2);
        
        public Liquidity OnPriceChange(TickPrice tickPrice)
        {
            Liquidity result = null;

            if (previousState == null)
                previousState = GetState();

            foreach (var it in intrinsicTimes)
            {
                it.OnPriceChange(tickPrice);
            }

            var currentState = GetState();
            
            if (previousState.Equals(currentState))
                return result;
            
            var surprise = new Surprise(tickPrice.Time, previousState, currentState, thresholds);
            surprises.Add(surprise);

            previousState = currentState;

            if (firstDayAfterWeekend == default(DateTime))
            {
                firstDayAfterWeekend = tickPrice.Time;
            }

            if (lastLiquidityCalcTime == default(DateTime))
            {
                lastLiquidityCalcTime = tickPrice.Time;
            }

            if (tickPrice.Time - lastLiquidityCalcTime > TimeSpan.FromDays(1.5)) // skip weekend
            {
                firstDayAfterWeekend = tickPrice.Time;
                lastLiquidityCalcTime = lastLiquidityCalcTime.Add(weekend);

                foreach (var item in slidingSurprises)
                {
                    item.MoveTime(weekend);
                }
            }

            slidingSurprises.AddLast(surprise);

            while (slidingSurprises.First().Time < tickPrice.Time - liquiditySlidingWindow)
            {
                slidingSurprises.RemoveFirst();
            }
            
            result = new Liquidity(tickPrice.Time, slidingSurprises.Sum(x => x.Value), slidingSurprises.Count);

            liquidities.Add(result);
            lastLiquidityCalcTime = tickPrice.Time;

            return result;
        }

        private LinkedList<Surprise> slidingSurprises = new LinkedList<Surprise>();
        
        public string GetStateString()
        {
            return string.Join("", intrinsicTimes.Select(x => (int)x.Mode));
        }

        /// <summary>
        /// Returns the network's state, where state[0] is for smallest threshold
        /// </summary>
        public NetworkState GetState()
        {
            return new NetworkState(intrinsicTimes.Select(x => x.Mode));
        }
    }
}
