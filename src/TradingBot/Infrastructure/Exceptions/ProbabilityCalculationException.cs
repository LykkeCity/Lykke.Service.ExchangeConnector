using System;

namespace TradingBot.Infrastructure.Exceptions
{
    public class ProbabilityCalculationException : Exception
    {
        public ProbabilityCalculationException(string message) : base(message)
        {

        }
    }
}
