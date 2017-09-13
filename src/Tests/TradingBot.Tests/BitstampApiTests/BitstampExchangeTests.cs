using System;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.Bitstamp;
using TradingBot.Infrastructure.Configuration;
using Xunit;

namespace TradingBot.Tests.BitstampApiTests
{
    public class BitstampExchangeTests
    {
        [Fact]
        public async Task PricesStreamTest()
        {
            var exchange = new BitstampExchange(new BitstampConfiguration()
            {
                Enabled = true,
                ApplicationKey = "de504dc5763aeef9ff52",
                Instruments = new [] { "BTCUSD" }
            }, null);

            bool result = await exchange.TestConnection();
            
            Assert.True(result);

            await exchange.OpenPricesStream();

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}