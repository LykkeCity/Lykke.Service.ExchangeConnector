using System;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public class GeometricalBrownianMotion
    {
        private readonly double sigma;
        private readonly double mu;
        private readonly double deltaT;
        private readonly Random random;
        
        private bool initiated;
        private double prevValue;

        /// <param name="initialValue">initial value</param>
        /// <param name="sigma">expected annual volatility</param>
        /// <param name="nYears">number of years</param>
        /// <param name="nGenerations">total number of generations</param>
        /// <param name="mu">yearly trend</param>
        /// <param name="random">the source of randomness</param>
        public GeometricalBrownianMotion(double initialValue, double sigma, double nYears, long nGenerations, double mu, Random random)
        {
            this.sigma = sigma;
            this.mu = mu;
            this.random = random;
            
            deltaT = nYears / nGenerations;
            
            prevValue = initialValue;
            initiated = false;
        }
        
        public double GenerateNextValue()
        {
            if (!initiated)
            {
                initiated = true;
            } 
            else 
            {
                prevValue += prevValue * (mu * deltaT + sigma * Math.Sqrt(deltaT) * random.NextGaussian());
            }
            
            return prevValue;
        }
    }
}
