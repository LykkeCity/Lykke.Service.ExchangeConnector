using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Bitfinex
{
    public class BitfinexExecutionHarvesterTest
    {
        private const string ApiKey = "";
        private const string ApiSecret = "";
        private const string WsApiUrl = "wss://api.bitfinex.com/ws";
        private readonly TestHandler _testHandler = new TestHandler();
        private readonly BitfinexExecutionHarvester _harvester;

        public BitfinexExecutionHarvesterTest()
        {
            ILog log = new LogToConsole();
            var bitfinexExchangeConfiguration = new BitfinexExchangeConfiguration
            {
                ApiKey = ApiKey,
                ApiSecret = ApiSecret,
                WebSocketEndpointUrl = WsApiUrl,
                SupportedCurrencySymbols = new[]{
                    new CurrencySymbol
                {
                    ExchangeSymbol = "LTCUSD",
                    LykkeSymbol = "LTCUSD"
                }}
            };

            var subscriber = new BitfinexWebSocketSubscriber(bitfinexExchangeConfiguration, true, log);
            _harvester = new BitfinexExecutionHarvester(subscriber, bitfinexExchangeConfiguration, _testHandler, log);
        }


        [Fact]
        public void ShouldReceiveExecution()
        {
            ExecutionReport result = null;
            var gate = new ManualResetEventSlim();
            _testHandler.Hook = trade =>
            {
                result = trade;
                return Task.CompletedTask;
            };
            _harvester.Start();
            gate.Wait();
        }

        private class TestHandler : IHandler<ExecutionReport>
        {
            public Func<ExecutionReport, Task> Hook;

            public Task Handle(ExecutionReport message)
            {
                return Hook(message);
            }
        }
    }
}
