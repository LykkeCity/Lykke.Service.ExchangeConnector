using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Infrastructure.Configuration;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Bitfinex
{
    public class BitfinexWebSocketSubscriberTest
    {
        private readonly BitfinexExchangeConfiguration _bitfinexExchangeConfiguration;
        private readonly ILog _log;
        private const string ApiKey = "";
        private const string ApiSecret = "";
        private const string WsApiUrl = "wss://api.bitfinex.com/ws";


        public BitfinexWebSocketSubscriberTest()
        {
            _log = new LogToConsole();
            _bitfinexExchangeConfiguration = new BitfinexExchangeConfiguration
            {
                ApiKey = ApiKey,
                ApiSecret = ApiSecret,
                WebSocketEndpointUrl = WsApiUrl
            };

        }

        [Fact]
        public void ShouldConnectToOpenFeed()
        {
            var subscriber = new BitfinexWebSocketSubscriber(_bitfinexExchangeConfiguration, false, _log);
            var gate = new ManualResetEventSlim(false);
            var response = "";
            subscriber.Subscribe(s =>
            {
                response = s;
                gate.Set();
                return Task.CompletedTask;
            });

            subscriber.Start();


            var recieved = gate.Wait(TimeSpan.FromSeconds(10));

            Assert.True(recieved);
            Assert.Equal("{\"event\":\"info\",\"version\":1.1}", response);
        }

        [Fact]
        public void ShouldConnectToAuthFeed()
        {
            var subscriber = new BitfinexWebSocketSubscriber(_bitfinexExchangeConfiguration, true, _log);

            var gate = new ManualResetEventSlim(false);
            var response = "";
            subscriber.Subscribe(s =>
            {
                response = s;
                gate.Set();
                return Task.CompletedTask;
            });

            subscriber.Start();


            var recieved = gate.Wait(TimeSpan.FromSeconds(10));

            Assert.True(recieved);
            Assert.NotEqual("{\"event\":\"auth\",\"status\":\"FAILED\",\"chanId\":0,\"code\":10100,\"msg\":\"apikey: invalid\"}", response);
        }
    }
}
