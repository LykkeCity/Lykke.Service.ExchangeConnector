using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
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
                SupportedCurrencySymbols = new[] {
                    new CurrencySymbol
                    {
                        LykkeSymbol = "BTCUSD",
                        ExchangeSymbol = "BTCUSD",
                    }},
                EndpointUrl = "",
                ApiKey = ""
            };

        private LykkeExchange Exchange => new LykkeExchange(config, null, new LogToConsole());

        [Fact]
        public async Task GetPairsTest()
        {
            Assert.True((await Exchange.GetAvailableInstruments(CancellationToken.None)).Any());
        }

        [Fact]
        public async Task OpenAndClosePrices()
        {
            var exchange = Exchange;
            var listForPrices = new List<TickPrice>();
            exchange.AddTickPriceHandler(new TickPriceHandler(listForPrices));
            
            exchange.Start();
            await Task.Delay(TimeSpan.FromSeconds(10));
            exchange.Stop();

            Assert.True(listForPrices.Any());
        }

        class TickPriceHandler : Handler<TickPrice>
        {
            private readonly List<TickPrice> list;

            public TickPriceHandler(List<TickPrice> list)
            {
                this.list = list;
            }
            
            public override Task Handle(TickPrice message)
            {
                list.Add(message);
                return Task.FromResult(0);
            }
        }
    }
}
