using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace TradingBot.Tests.BitMex
{
    public class BitfinexWebSocketTest : IDisposable
    {
        private readonly WebSocketTextMessenger _clientWebSocket;
        private const string ApiUrl = @"wss://api.bitfinex.com/ws";
        private readonly ITestOutputHelper _output;

        public BitfinexWebSocketTest(ITestOutputHelper output)
        {
            _output = output;
            _clientWebSocket = new WebSocketTextMessenger(ApiUrl, new LogToConsole());

        }

        [Fact]
        public async Task Connect()
        {
            await _clientWebSocket.ConnectAsync(CancellationToken.None);

            var r = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            var respose = JsonConvert.DeserializeObject<InfoResponse>(r);
            Assert.NotNull(respose);
            Assert.NotNull(respose.Event);
        }


        [Fact]
        public async Task ReceiveOrderBook()
        {
            await _clientWebSocket.ConnectAsync(CancellationToken.None);
            var info = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            Assert.NotNull(info);

            var request = SubscribeOrderBooksRequest.BuildRequest("BTCUSD", "", "R0");

            await _clientWebSocket.SendRequestAsync(request, CancellationToken.None);

            var successfull = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            var respose = JsonConvert.DeserializeObject<SubscribedResponse>(successfull);

            var snapshot = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            OrderBookSnapshotResponse.Parse(snapshot);

            var update = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            OrderBookUpdateResponse.Parse(update);

            Assert.NotNull(respose);
        }

        [Fact]
        public async Task SubscribeToTickers()
        {
            await _clientWebSocket.ConnectAsync(CancellationToken.None);
            var info = await _clientWebSocket.GetResponseAsync(CancellationToken.None);

            Assert.NotNull(info);

            var request = SublscribeTickeRequest.BuildRequest("BTCUSD");

            await _clientWebSocket.SendRequestAsync(request, CancellationToken.None);

            var successfull = await _clientWebSocket.GetResponseAsync(CancellationToken.None);
            var respose = JsonConvert.DeserializeObject<SubscribedResponse>(successfull);
            var ticker = await _clientWebSocket.GetResponseAsync(CancellationToken.None);
            TickerResponse.Parse(ticker);

            Assert.NotNull(respose);
        }

        public void Dispose()
        {
            _clientWebSocket?.Dispose();
        }
    }

}

