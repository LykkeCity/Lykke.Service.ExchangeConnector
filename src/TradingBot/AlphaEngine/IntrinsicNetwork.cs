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
        public IntrinsicNetwork(int dimension, params decimal[] thresholds)
        {
            if (dimension < 1)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be greater then zero");

            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));

            if (thresholds.Length < dimension)
                throw new ArgumentException(nameof(dimension), "Number of thresholds must be the same as dimension");

            for (int i = 0; i < dimension - 1; i++)
            {
                if (thresholds[i] > thresholds[i+1])
                {
                    throw new ArgumentException(nameof(thresholds), "Thresholds must be in order from smallest to the bigger");
                }
            }

            this.dimension = dimension;
            this.thresholds = thresholds;
        }

        public IntrinsicNetwork(int dimension, decimal firstThreshold)
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
        }

        private int dimension;

        public int Dimension => dimension;


        private decimal[] thresholds;
        private List<IntrinsicTime> intrinsicTimes;
        private List<Surprise> surprises;

        public void Init()
        {
            intrinsicTimes = new List<IntrinsicTime>(dimension);
            
            foreach (var threshold in thresholds)
            {
                intrinsicTimes.Add(new IntrinsicTime(threshold));
            }

            surprises = new List<Surprise>();
        }

        public void OnPriceChange(PriceTime priceTime)
        {
            //var tasks = intrinsicTimes.Select(x => Task.Run(() => x.HandlePriceChange(price, time)));
            //Task.WaitAll(tasks.ToArray());

            //intrinsicTimes.AsParallel().ForAll(it => it.HandlePriceChange(price, time));

            var previousState = GetState();

            foreach (var it in intrinsicTimes)
            {
                it.OnPriceChange(priceTime);
            }

            var currentState = GetState();
            surprises.Any();

            bool equals = true;
            for (int i = 0; i < previousState.Length; i++)
            {
                if (previousState[i] != currentState[i])
                {
                    equals = false;
                    break;
                }
            }

            if (!equals)
            {
                surprises.Add(new Surprise(priceTime.Time,
                    ProbabilityIndicator.Calculate(previousState, currentState, thresholds)));
            }
        }

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
        public BitArray GetState()
        {
            return new BitArray(intrinsicTimes.Select(x => x.Mode == AlgorithmMode.Up).ToArray());
        }
    }
}
