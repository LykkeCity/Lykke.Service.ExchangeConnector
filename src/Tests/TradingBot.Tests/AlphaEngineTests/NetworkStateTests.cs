using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class NetworkStateTests
    {
        [Fact]
        public void CalcFirstDifference_DifferenceInThirdPosition_Return2()
        {
            var result = new NetworkState(new[] { false, false, true, false }).FirstDifference();
            
            Assert.Equal(2, result);
        }

        [Fact]
        public void CalcFirstDifference_DifferenceInSecondPosition_Return1()
        {
            var result = new NetworkState(new[] { true, false, true, false }).FirstDifference();

            Assert.Equal(1, result);
        }

        [Fact]
        public void CalcFirstDifference_NoDifferences_ReturnOne()
        {
            var result = new NetworkState(new[] { false, false, false, false }).FirstDifference();
            
            Assert.Equal(1, result);
        }

        [Fact]
        public void Equals_StatesAreEquals_True()
        {
            var state1 = new NetworkState(true, true, false, false);
            var state2 = new NetworkState(true, true, false, false);

            Assert.True(state1.Equals(state2));
            Assert.True(state2.Equals(state1));
        }

        [Fact]
        public void Equals_StatesAreDifferent_False()
        {
            var state1 = new NetworkState(false, true, false, false);
            var state2 = new NetworkState(true, true, false, false);

            Assert.False(state1.Equals(state2));
            Assert.False(state2.Equals(state1));
        }

        [Fact]
        public void ConstructFromInt_Test1()
        {
            var state = new NetworkState(dimension: 2, state: 1);

            Assert.Equal("10", state.ToString());
            Assert.Equal(1, state.ToInteger());
        }

        [Fact]
        public void ConstructFromInt_Test2()
        {
            var state = new NetworkState(dimension: 2, state: 2);

            Assert.Equal("01", state.ToString());
            Assert.Equal(2, state.ToInteger());
        }
        
        [Fact]
        public void ConstructFromInt_Test3()
        {
            var state = new NetworkState(dimension: 2, state: 3);

            Assert.Equal("11", state.ToString());
            Assert.Equal(3, state.ToInteger());
        }

        [Fact]
        public void ConstructFromInt_Test0()
        {
            var state = new NetworkState(dimension: 2, state: 0);

            Assert.Equal("00", state.ToString());
            Assert.Equal(0, state.ToInteger());
        }

        [Fact]
        public void ConstructWithBoolArray_Test()
        {
            var state = new NetworkState(true, true, false, false);

            Assert.Equal("1100", state.ToString());
            Assert.Equal(3, state.ToInteger());
        }
    }
}
