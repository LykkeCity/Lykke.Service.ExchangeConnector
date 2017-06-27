using System;

namespace TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine
{
    public class Liquidity
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="totalSurprise"></param>
        /// <param name="K">
        /// The number of transitions within time interval on the intrinsic network,
        /// in fact equals to the sum of all directional changes related to thresholds 
        /// δ1; ... ; δn that occurred within time interval, representing
        /// the measurement of activity across multiple scales.
        /// </param>
        public Liquidity(DateTime time, double totalSurprise, int K)
        {
            Time = time;

            var argument = (totalSurprise - K * H1) / Math.Sqrt(K * H2);
            Value = 1 - Phi(argument);
        }

        public DateTime Time { get; }
        public double Value { get; }

        public override string ToString()
        {
            return $"{Time}, L={Value}";
        }


        /// <summary>
        /// First order informativeness
        /// </summary>
        private const double H1 = 0.4604;

        /// <summary>
        /// Second oreder informativeness
        /// </summary>
        private const double H2 = 0.70818;
        

        public static double Calculate(double totalSurprise, int K)
        {
            var argument = (totalSurprise - K * H1) / Math.Sqrt(K * H2);
            
            return 1 - Phi(argument);
        }

        /// <summary>
        /// The function Φ(x) is the cumulative density function (CDF) 
        /// of a standard normal (Gaussian) random variable
        /// see: https://www.johndcook.com/blog/csharp_phi/
        /// </summary>
        public static double Phi(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }
    }
}
