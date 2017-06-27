using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
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

            bool stateChanged = false;

            var surprisesToAddToSlidings = new List<Surprise>();
            foreach (var it in intrinsicTimes)
            {
               
                it.OnPriceChange(tickPrice);

                var intermediateState = GetState();

                if (!previousState.Equals(intermediateState))
                {
                    stateChanged = true;

                    var surprise = new Surprise(tickPrice.Time, previousState, intermediateState, thresholds);
                    surprises.Add(surprise);
                    surprisesToAddToSlidings.Add(surprise);

                    previousState = intermediateState;
                }
            }
            
            if (!stateChanged)
                return result;

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


            surprisesToAddToSlidings.ForEach(x => slidingSurprises.AddLast(x));

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
