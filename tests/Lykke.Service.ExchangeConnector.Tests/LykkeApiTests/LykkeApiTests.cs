using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Moq;
using TradingBot.Exchanges.Concrete.LykkeExchange;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.LykkeApiTests
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

        private LykkeExchange Exchange;
        private readonly IHandler<ExecutionReport> _tradeHandler;

        public LykkeApiTests()
        {
            var tickPriceHandler = new Mock<IHandler<TickPrice>>().Object;
            _tradeHandler = new Mock<IHandler<ExecutionReport>>().Object;
            Exchange = new LykkeExchange(config, null, tickPriceHandler, _tradeHandler, new LogToConsole());
        }


        [Fact]
        public async Task GetPairsTest()
        {
            Assert.True((await Exchange.GetAvailableInstruments(CancellationToken.None)).Any());
        }

        [Fact]
        public async Task OpenAndClosePrices()
        {
            var listForPrices = new List<TickPrice>();

            var tickPriceHandler = new TickPriceHandler(listForPrices);
            Exchange = new LykkeExchange(config, null, tickPriceHandler, _tradeHandler, new LogToConsole());

            var exchange = Exchange;

            exchange.Start();
            await Task.Delay(TimeSpan.FromSeconds(10));
            exchange.Stop();

            Assert.True(listForPrices.Any());
        }

        class TickPriceHandler : IHandler<TickPrice>
        {
            private readonly List<TickPrice> list;

            public TickPriceHandler(List<TickPrice> list)
            {
                this.list = list;
            }

            public Task Handle(TickPrice message)
            {
                list.Add(message);
                return Task.FromResult(0);
            }
        }
    }
}
