using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using Xunit;
using Xunit.Abstractions;

namespace TradingBot.Tests.BitMex
{
    public class BinMexWebSocketTest : IDisposable
    {
        private readonly ClientWebSocket _clientWebSocket;
        private const string ApiUrl = @"wss://testnet.bitmex.com/realtime";
        private readonly ITestOutputHelper _output;

        public BinMexWebSocketTest(ITestOutputHelper output)
        {
            _output = output;
            _clientWebSocket = new ClientWebSocket();

        }

        [Fact]
        public async Task Connect()
        {
            var cts = new CancellationTokenSource();

            await _clientWebSocket.ConnectAsync(new Uri(ApiUrl), cts.Token);
            var msg = EncodeText("help");

            await _clientWebSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, cts.Token);

            var buf = new byte[1000];
            var resp = new ArraySegment<byte>(buf);
            var rr = await _clientWebSocket.ReceiveAsync(resp, cts.Token);

            var respose = DecodeText(resp.Array.Take(rr.Count).ToArray());
            _output.WriteLine(respose);
        }


        [Fact]
        public async Task ReceivePrices()
        {
            await _clientWebSocket.ConnectAsync(new Uri(ApiUrl), CancellationToken.None);

            //var msg = EncodeText("{\"op\": \"subscribe\", \"args\": [\"quote\"]}");
            var msg = EncodeText("{\"op\": \"subscribe\", \"args\": [\"orderBookL2:XBTUSD\"]}");

            await _clientWebSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[10000];
            var segment = new ArraySegment<byte>(buffer);

            var sb = new StringBuilder();
            int counter = 0;
            while (true)
            {
                var endOfMessage = false;
                while (!endOfMessage)
                {
                    var re = await _clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                    sb.Append(DecodeText(segment.Array.Take(re.Count).ToArray()));
                    endOfMessage = re.EndOfMessage;
                }

                if (counter > 1)
                {
                    var wholeMessage = sb.ToString();
                    var resp = JsonConvert.DeserializeObject<TableResponse>(wholeMessage);
                    //  HandleResponse(resp);
                }
                counter++;
                sb.Length = 0;

                if (counter == 100)
                {
                    break;
                }
            }
        }


        private byte[] EncodeText(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        private string DecodeText(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }

        public void Dispose()
        {
            _clientWebSocket?.Dispose();
        }
    }

    public class Response
    {
        [JsonProperty("table")]
        public string Table { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("data")]
        public Quote[] Data { get; set; }
    }

    public class Quote
    {
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("bidSize")]
        public long? BidSize { get; set; }

        [JsonProperty("bidPrice")]
        public decimal? BidPrice { get; set; }

        [JsonProperty("askPrice")]
        public decimal? AskPrice { get; set; }

        [JsonProperty("askSize")]
        public long? AskSize { get; set; }
    }
}

