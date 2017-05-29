using System;
using TradingBot.AlphaEngine;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests.AlphaEngineTests
{
    public class IntrinsicTimeTests
    {
        [Fact]
        public void ThresholdOnePercent_PriceGoDownFrom100To99_DirectionalChange()
        {
            var threshold = .01m;
            var intrinsicTime = new IntrinsicTime(threshold);

            var price1 = new PriceTime(100m, DateTime.Now.AddMinutes(-1));
            var price2 = new PriceTime(99m, DateTime.Now);


            var event1 = intrinsicTime.OnPriceChange(price1);
            var event2 = intrinsicTime.OnPriceChange(price2);

            Assert.Null(event1);
            Assert.IsType<DirectionalChange>(event2);
            Assert.Equal(AlgorithmMode.Down, event2.Mode);
            Assert.Equal(AlgorithmMode.Down, intrinsicTime.Mode);
        }

        [Fact]
        public void ThresholdOnePercent_PriceGoDownFrom100To99_1_None()
        {
            var threshold = .01m;
            var intrinsicTime = new IntrinsicTime(threshold);

            var price1 = new PriceTime(100m, DateTime.Now.AddMinutes(-1));
            var price2 = new PriceTime(99.1m, DateTime.Now);


            var event1 = intrinsicTime.OnPriceChange(price1);
            var event2 = intrinsicTime.OnPriceChange(price2);

            Assert.Null(event1);
            Assert.Null(event2);
        }
        
        [Fact]
        public void ThresholdOnePercent_PriceGoUpFrom100To103_Overshoot()
        {
            var threshold = .01m;
            var intrinsicTime = new IntrinsicTime(threshold);

            var price1 = new PriceTime(100m, DateTime.Now.AddMinutes(-1));
            var price2 = new PriceTime(103m, DateTime.Now);


            var event1 = intrinsicTime.OnPriceChange(price1);
            var event2 = intrinsicTime.OnPriceChange(price2);

            Assert.Null(event1);
            Assert.IsType<Overshoot>(event2);
            Assert.Equal(AlgorithmMode.Up, event2.Mode);
        }
    }
}
