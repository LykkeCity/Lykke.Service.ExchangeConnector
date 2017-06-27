using System;
using TradingBot.Common.Trading;
using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class IntrinsicNetworkTests
    {
        [Fact]
        public void PriceGoDownInFirstThreshold_StateChangesTo01()
        {
            var network = new IntrinsicNetwork(
                dimension: 2, 
                firstThreshold: 0.01m, 
                liquiditySlidingWindow: TimeSpan.FromMinutes(1));
            
            var price1 = new TickPrice(DateTime.Now.AddMinutes(-1), 100m);
            var price2 = new TickPrice(DateTime.Now, 99m);


            network.OnPriceChange(price1);
            var state1 = network.GetState();

            network.OnPriceChange(price2);
            var state2 = network.GetState();


            Assert.Equal("11", state1.ToString());
            Assert.Equal("01", state2.ToString());
        }

        [Fact]
        public void PriceGoDownInBothThreshold_StateChangesTo00()
        {
            var network = new IntrinsicNetwork(
                dimension: 2,
                firstThreshold: 0.01m,
                liquiditySlidingWindow: TimeSpan.FromMinutes(1));

            var price1 = new TickPrice(DateTime.Now.AddMinutes(-1), 100m);
            var price2 = new TickPrice(DateTime.Now, 98m);


            network.OnPriceChange(price1);
            var state1 = network.GetState();

            network.OnPriceChange(price2);
            var state2 = network.GetState();


            Assert.Equal("11", state1.ToString());
            Assert.Equal("00", state2.ToString());
        }
        
        [Fact]
        public void PriceGoUp_StateNotChanged()
        {
            var network = new IntrinsicNetwork(
                dimension: 2,
                firstThreshold: 0.01m,
                liquiditySlidingWindow: TimeSpan.FromMinutes(1));

            var price1 = new TickPrice(DateTime.Now.AddMinutes(-1), 100m);
            var price2 = new TickPrice(DateTime.Now, 110m);


            network.OnPriceChange(price1);
            var state1 = network.GetState();

            network.OnPriceChange(price2);
            var state2 = network.GetState();


            Assert.Equal("11", state1.ToString());
            Assert.Equal("11", state2.ToString());
        }
    }
}
