using System;
using System.Collections.Generic;
using System.Text;
using TradingBot.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class ProbabilityIndicatorTests
    {
        [Fact]
        public void CalcFirstDifference_DifferenceInThirdPosition_Return2()
        {
            var result = ProbabilityIndicator.CalculateFirstDifference(new byte[] { 0, 0, 1, 0 });

            Assert.Equal(2, result);
        }


        [Fact]
        public void CalcFirstDifference_NoDifferences_ReturnOne()
        {
            var result = ProbabilityIndicator.CalculateFirstDifference(new byte[] { 0, 0, 0, 0 });

            Assert.Equal(1, result);
        }

        [Fact]
        public void CalculateCase1()
        {
            var prevState = new byte[] { 0, 1, 1, 1};
            var curState = new byte[] { 1, 1, 1, 1 };
            var deltas = new decimal[] { 0.00025m, 0.0005m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);

            Assert.Equal(0.36787944117144233, result);
        }

        [Fact]
        public void CalculateCase2()
        {
            var prevState = new byte[] { 0, 1, 1, 1 };
            var curState = new byte[] { 0, 0, 1, 1 };
            var deltas = new decimal[] { 0.00025m, 0.0005m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);
            
            Assert.Equal(0.63212055882855767, result);
        }

        [Fact]
        public void CalculateCase3()
        {
            var prevState = new byte[] { 0, 0, 1, 1 };
            var curState = new byte[] { 0, 0, 0, 1 };
            var deltas = new decimal[] { 0.00025m, 0.0005m, 0.001m };

            var result = ProbabilityIndicator.Calculate(prevState, curState, deltas);

            Assert.Equal(0.84348235725033427, result);
        }
    }
}
