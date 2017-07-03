using System;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public class GeometricalBrownianMotion
    {
        private double sigma;
        private double mu;
        private double deltaT;
        private double prevValue;
        private Random rand;
        private bool initiated;

        /// <param name="initialValue">initial value</param>
        /// <param name="sigma">expected annual volatility</param>
        /// <param name="nYears">number of years</param>
        /// <param name="nGenerations">total number of generations</param>
        /// <param name="mu">yearly trend</param>
        public GeometricalBrownianMotion(double initialValue, double sigma, double nYears, long nGenerations, double mu)
        {
            this.sigma = sigma;
            this.mu = mu;
            deltaT = nYears / nGenerations;
            prevValue = initialValue;
            rand = new Random();
            initiated = false;
        }
        
        public double GenerateNextValue()
        {
            if (!initiated)
            {
                initiated = true;
            } else 
            {
                prevValue += prevValue * (mu * deltaT + sigma * Math.Sqrt(deltaT) * rand.NextGaussian());
            }
            return prevValue;
        }
    }
}