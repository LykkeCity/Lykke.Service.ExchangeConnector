using Microsoft.Extensions.Logging;
using System;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Common.Infrastructure;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// Calculated as in paper "Multi-scale Representation of High Frequency Market
    /// Liquidity", p. 10
    /// </summary>
    public class ProbabilityIndicator
    {
        private static ILogger Logger = Logging.CreateLogger<ProbabilityIndicator>();
        
        public static double Calculate(
            NetworkState previousState, 
            NetworkState currentState,
            decimal[] deltas)
        {
            double result = 0;

            int firstDifference = previousState.FirstDifference();

            if (firstDifference == 1)
            {
                var exp = Math.Exp(decimal.ToDouble(-(deltas[1] - deltas[0]) / deltas[0]));
                if (previousState[0] != currentState[0])
                {
                    result = 1 - exp;
                }
                else if (previousState[1] != currentState[1])
                {
                    result = exp;
                }
                else
                {
                    throw new ProbabilityCalculationException($"Unexpected Probabilty Indicator state: {previousState} -> {currentState}");
                }
            }
            else if (firstDifference > 1)
            {
                double numerator = 1;
                for (int i = 1; i <= firstDifference; i++)
                {
                    numerator *= Math.Exp(decimal.ToDouble(- (deltas[i] - deltas[i-1]) / deltas[i-1]));
                }
                
                double sum = 0;
                for (int i = 1; i < firstDifference; i++)
                {
                    double firstVal = 1 - Math.Exp(decimal.ToDouble(-(deltas[i] - deltas[i - 1]) / deltas[i - 1]));
                    double mult = 1;

                    for (int j = i + 1; j <= firstDifference; j++)
                    {
                        mult *= Math.Exp(decimal.ToDouble(-(deltas[j] - deltas[j - 1]) / deltas[j - 1]));
                    }
                    sum += firstVal * mult;
                }
                
                double denominator = 1 - sum;

                if (currentState[0] != previousState[0])
                {
                    result = 1 - numerator / denominator;
                }
                else if (currentState[firstDifference] != previousState[firstDifference])
                {
                    result = numerator / denominator;
                }
            }
            else
            {
                throw new ProbabilityCalculationException($"Unexpected {nameof(firstDifference)} value {firstDifference}");
            }

            return result;
        }
    }
}
