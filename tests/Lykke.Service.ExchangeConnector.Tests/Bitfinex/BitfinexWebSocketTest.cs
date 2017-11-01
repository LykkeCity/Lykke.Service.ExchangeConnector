using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using Xunit;
using Xunit.Abstractions;
using SubscribeRequest = TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model.SubscribeRequest;

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
            _clientWebSocket = new WebSocketTextMessenger(ApiUrl, new LogToConsole(), CancellationToken.None);

        }

        [Fact]
        public async Task Connect()
        {
            await _clientWebSocket.ConnectAsync();

            var r = await _clientWebSocket.GetResponseAsync();

            var respose = JsonConvert.DeserializeObject<InfoResponse>(r);
            Assert.NotNull(respose);
            Assert.NotNull(respose.Event);
        }


        [Fact]
        public async Task ReceiveOrderBook()
        {
            await _clientWebSocket.ConnectAsync();
            var info = await _clientWebSocket.GetResponseAsync();

            Assert.NotNull(info);

            var request = new SubscribeRequest
            {
                Event = "subscribe",
                Channel = "book",
                Pair = "BTCUSD",
                Prec = "R0"
            };
            await _clientWebSocket.SendRequestAsync(request);

            var successfull = await _clientWebSocket.GetResponseAsync();

            var respose = JsonConvert.DeserializeObject<SubscribedResponse>(successfull);

            var snapshot = await _clientWebSocket.GetResponseAsync();

            var obsh = OrderBookSnapshotResponse.Parse(snapshot);


            var update = await _clientWebSocket.GetResponseAsync();

            var upd = OrderBookUpdateResponse.Parse(update);

            Assert.NotNull(respose);


        }


        [Fact]
        public void Test2()
        {
            var dt = DateTime.UtcNow;
            var dts = JsonConvert.SerializeObject(dt, new JsonSerializerSettings()
            {
              //  DateFormatHandling = DateFormatHandling.IsoDateFormat,
               // DateTimeZoneHandling = DateTimeZoneHandling.Utc
                // DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffzzz"
            });

               
            var dd = JsonConvert.DeserializeObject<DateTime>("\"2017-10-25T10:23:17.000+0000\"");
            var dd2 = JsonConvert.DeserializeObject<DateTime>("\"2017-10-25T10:23:17.000+00:00\"");

            Assert.Equal(dd, dd2);
        }

        public void Dispose()
        {
            _clientWebSocket?.Dispose();
        }
    }

}

