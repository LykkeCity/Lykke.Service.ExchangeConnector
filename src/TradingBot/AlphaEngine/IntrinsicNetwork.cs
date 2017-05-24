using System;
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

        public void Init()
        {
            intrinsicTimes = new List<IntrinsicTime>(dimension);
            
            foreach (var threshold in thresholds)
            {
                intrinsicTimes.Add(new IntrinsicTime(threshold));
            }
        }

        public void OnPriceChange(PriceTime priceTime)
        {
            //var tasks = intrinsicTimes.Select(x => Task.Run(() => x.HandlePriceChange(price, time)));
            //Task.WaitAll(tasks.ToArray());

            //intrinsicTimes.AsParallel().ForAll(it => it.HandlePriceChange(price, time));

            foreach (var it in intrinsicTimes)
            {
                it.OnPriceChange(priceTime);
            }
        }

        public string GetStateString()
        {
            return string.Join("", intrinsicTimes.Select(x => (int)x.Mode));
        }

        /// <summary>
        /// Returns the network's state, where state[0] is for smallest threshold
        /// </summary>
        /// <returns></returns>
        public byte[] GetState()
        {
            return intrinsicTimes.Select(x => (byte)x.Mode).ToArray();
        }
    }
}
