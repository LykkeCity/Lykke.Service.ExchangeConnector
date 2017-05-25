using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using TradingBot.Infrastructure;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.AlphaEngine
{
    /// <summary>
    /// Calculated as in paper "Multi-scale Representation of High Frequency Market
    /// Liquidity", p. 10
    /// </summary>
    public class ProbabilityIndicator
    {
        private static ILogger Logger = Logging.CreateLogger<ProbabilityIndicator>();

        public static int CalculateFirstDifference(BitArray state)
        {
            int i;
            for (i = 1; i < state.Length && state[i] == state[0]; i++)
            {
            }

            int firstDifference = i < state.Length ? i : 1;

            return firstDifference;
        }

        public static double Calculate(BitArray previousState, 
            BitArray currentState,
            decimal[] deltas)
        {
            double result = 0;

            int firstDifference = CalculateFirstDifference(previousState);

            if (firstDifference == 1)
            {
                var exp = Math.Exp(decimal.ToDouble(-(deltas[1] - deltas[0]) / deltas[0]));
                if (previousState[0] != currentState[0])
                {
                    result = exp;
                }
                else if (previousState[1] != currentState[1])
                {
                    result = 1 - exp;
                }
                else
                {
                    throw new ProbabilityCalculationException("Unexpected Probabilty Indicator state");
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
                    double firstVal = Math.Exp(decimal.ToDouble(-(deltas[i] - deltas[i - 1]) / deltas[i - 1]));
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
                    result = numerator / denominator;
                }
                else if (currentState[firstDifference] != previousState[firstDifference])
                {
                    result = 1 - numerator / denominator;
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
