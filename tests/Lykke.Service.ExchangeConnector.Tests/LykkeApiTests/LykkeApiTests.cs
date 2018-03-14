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
using OrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;

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
            var orderBookHandler = new Mock<IHandler<OrderBook>>().Object;
            _tradeHandler = new Mock<IHandler<ExecutionReport>>().Object;
            Exchange = new LykkeExchange(config, null, tickPriceHandler, orderBookHandler, _tradeHandler, new LogToConsole());
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
            var listForBooks = new List<OrderBook>();

            var tickPriceHandler = new TickPriceHandler(listForPrices);
            var orderBookHandler = new OrderBookHandler(listForBooks);
            Exchange = new LykkeExchange(config, null, tickPriceHandler, orderBookHandler, _tradeHandler, new LogToConsole());

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

        class OrderBookHandler : IHandler<OrderBook>
        {
            private readonly List<OrderBook> list;

            public OrderBookHandler(List<OrderBook> list)
            {
                this.list = list;
            }

            public Task Handle(OrderBook message)
            {
                list.Add(message);
                return Task.FromResult(0);
            }
        }
    }
}
