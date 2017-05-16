using System;
using System.Collections.Generic;
using System.Linq;

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

        private int dimension;

        public int Dimension => dimension;


        private decimal[] thresholds;

        private List<IntrinsicTime> intrinsicTimes;

        public void Init()
        {
            intrinsicTimes = new List<IntrinsicTime>(dimension);

            int i = 0;
            foreach (var threshold in thresholds)
            {
                intrinsicTimes.Add(new IntrinsicTime($"{i++}", threshold));
            }
        }

        public void HandlePrice(decimal price, DateTime time)
        {
            foreach (var it in intrinsicTimes)
            {
                it.HandlePriceChange(price, time);
            }
        }

        public string GetStateString()
        {
            var defaultMode = AlgorithmMode.Up;

            return string.Join("", intrinsicTimes.Select(x => (int)(x.LastDirectionalChange?.Mode ?? defaultMode)));
        }
    }
}
