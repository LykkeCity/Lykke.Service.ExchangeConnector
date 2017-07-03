using System;
namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public static class NextGaussianRandomExtension
    {
        private static double mean = 0.0;
        private static double stdDev = 1.0;
        
        public static double NextGaussian(this Random rand)
        {
            double u1 = 1.0-rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0-rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

            return randNormal;
        }
    }
}