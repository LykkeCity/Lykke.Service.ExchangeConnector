using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.LykkeExchange;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace TradingBot.Tests.LykkeApiTests
{
    public class LykkeApiTests
    {
        private readonly LykkeExchangeConfiguration config = new LykkeExchangeConfiguration()
            {
                Instruments = new [] { "BTCUSD" },
                EndpointUrl = ""
            };

        private LykkeExchange Exchange => new LykkeExchange(config, null);
        
        [Fact]
        public async Task IsAliveTest()
        {
            Assert.True(await Exchange.TestConnection());
        }

        [Fact]
        public async Task GetPairsTest()
        {
            Assert.True((await Exchange.GetAvailableInstruments(CancellationToken.None)).Any());
        }

        [Fact]
        public async Task OpenAndClosePrices()
        {
            var exchange = Exchange;
            var listForPrices = new List<InstrumentTickPrices>();
            exchange.AddTickPriceHandler(new TickPriceHandler(listForPrices));
            
            await exchange.OpenPricesStream();
            await Task.Delay(TimeSpan.FromSeconds(10));
            await exchange.ClosePricesStream();

            Assert.True(listForPrices.Any());
        }

        class TickPriceHandler : Handler<InstrumentTickPrices>
        {
            private readonly List<InstrumentTickPrices> list;

            public TickPriceHandler(List<InstrumentTickPrices> list)
            {
                this.list = list;
            }
            
            public override Task Handle(InstrumentTickPrices message)
            {
                list.Add(message);
                return Task.FromResult(0);
            }
        }
    }
}