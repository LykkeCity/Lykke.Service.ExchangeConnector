using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        public void OnPriceChange(PriceTime priceTime)
        {
            var previousState = GetState();

            foreach (var it in intrinsicTimes)
            {
                it.OnPriceChange(priceTime);
            }

            var currentState = GetState();
            
            if (previousState.Equals(currentState))
                return;

            var surprise = new Surprise(priceTime.Time,
                previousState, currentState, thresholds);

            surprises.Add(surprise);


            TimeSpan liquidityResolution = TimeSpan.FromMinutes(1);
            TimeSpan weekend = TimeSpan.FromDays(2);

            //if (firstDayAfterWeekend == default(DateTime))
            //{
            //    firstDayAfterWeekend = priceTime.Time;
            //}

            if (lastLiquidityCalcTime == default(DateTime))
            {
                lastLiquidityCalcTime = priceTime.Time;
            }

            if (priceTime.Time - lastLiquidityCalcTime > weekend) // skip weekend
            {
                //firstDayAfterWeekend = priceTime.Time;
                lastLiquidityCalcTime = lastLiquidityCalcTime.Add(weekend);

                foreach (var item in slidingSurprises)
                {
                    item.MoveTime(weekend);
                }
            }

            slidingSurprises.AddLast(surprise);

            if (priceTime.Time - lastLiquidityCalcTime >= liquidityResolution)
            {
                var value = Liquidity.Calculate(
                    slidingSurprises.Sum(x => x.Value),
                    slidingSurprises.Count());

                liquidities.Add(new Liquidity(priceTime.Time, value));

                lastLiquidityCalcTime = priceTime.Time;

                while (slidingSurprises.Any() && slidingSurprises.First().Time < priceTime.Time - liquiditySlidingWindow)
                    slidingSurprises.RemoveFirst();

                //while (slidingSurprises.Any() && (slidingSurprises.First().Time < firstDayAfterWeekend 
                //        ? slidingSurprises.First().Time.Add(weekend) 
                //        : slidingSurprises.First().Time) 
                //                < priceTime.Time - liquiditySlidingWindow)
                //{
                //    slidingSurprises.RemoveFirst();
                //}   
            }
        }

        private LinkedList<Surprise> slidingSurprises = new LinkedList<Surprise>();
        
        /// <summary>
        /// The number of transitions within time interval on the intrinsic network,
        /// in fact equals to the sum of all directional changes related to thresholds 
        /// δ1; ... ; δn that occurred within time interval, representing
        /// the measurement of activity across multiple scales.
        /// </summary>
        /// <param>Time interval</param>
        public int CalcK(DateTime from, DateTime to)
        {
            return intrinsicTimes.SelectMany(x => x.IntrinsicTimeEvents)
                .OfType<DirectionalChange>()
                .Where(x => from <= x.Time && x.Time <= to)
                .Count();
        }

        public double CalcTotalSurprise(DateTime from, DateTime to)
        {
            return surprises.Where(x => from <= x.Time && x.Time <= to)
                .Sum(x => x.Value);
        }

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
