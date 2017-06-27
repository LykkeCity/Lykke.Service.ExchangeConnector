using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class ProbabilityIndicatorTests
    {
        [Fact]
        public void CalculateCase1()
        {
            var prevState = new NetworkState(new[] { false, true, true, true});
            var curState = new NetworkState(new[] { true, true, true, true });
            var deltas = new decimal[] { 0.00025m, 0.0005m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);

            Assert.Equal(0.63212055882855767, result);
        }

        [Fact]
        public void CalculateCase2()
        {
            var prevState = new NetworkState(new[] { false, true, true, true });
            var curState = new NetworkState(new[] { false, false, true, true });
            var deltas = new decimal[] { 0.00025m, 0.0005m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);
            
            Assert.Equal(0.36787944117144233, result);
        }

        [Fact]
        public void CalculateCase3()
        {
            var prevState = new NetworkState(new[] { false, false, true, true });
            var curState = new NetworkState(new[] { false, false, false, true });
            var deltas = new decimal[] { 0.00025m, 0.0005m, 0.001m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);

            Assert.Equal(0.84348235725033427, result);
        }

        [Fact]
        public void SumOfAllProbabilitiesIsOne()
        {
            var initState = new NetworkState(dimension: 2, state: 0);
            
            var possibleState1 = new NetworkState(dimension: 2, state: 1);
            var possibleState2 = new NetworkState(dimension: 2, state: 2);
            var possibleState3 = new NetworkState(dimension: 2, state: 3);
            
            var deltas = new decimal[] { 0.00025m, 0.0005m };

            var p1 = ProbabilityIndicator.Calculate(initState, possibleState1, deltas);
            var p2 = ProbabilityIndicator.Calculate(initState, possibleState2, deltas);
            var p3 = ProbabilityIndicator.Calculate(initState, possibleState3, deltas);
            
            Assert.Equal(1, p1 + p2 + p3);
        }
    }
}
