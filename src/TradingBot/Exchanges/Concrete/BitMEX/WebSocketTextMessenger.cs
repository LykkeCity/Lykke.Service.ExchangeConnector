using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using Polly;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    public sealed class WebSocketTextMessenger : IDisposable
    {
        private readonly string _endpointUrl;
        private readonly ILog _log;
        private ClientWebSocket _clientWebSocket;
        private readonly TimeSpan _responseTimeout = TimeSpan.FromMinutes(1);
        private CancellationTokenSource _globalCancellationTokenSource;

        public WebSocketTextMessenger(string endpointUrl, ILog log)
        {

            _endpointUrl = endpointUrl;
            _log = log;

        }

        public void Dispose()
        {
            _clientWebSocket?.Dispose();
            _globalCancellationTokenSource?.Dispose();
        }


        public async Task Connect()
        {
            _globalCancellationTokenSource = new CancellationTokenSource();

            await _log.WriteInfoAsync(nameof(Connect), "Connecting to WebSocket", $"Bitmex API {_endpointUrl}");
            var uri = new Uri(_endpointUrl);

            const int attempts = 100;
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(attempts, attempt => TimeSpan.FromSeconds(3));
            try
            {
                _clientWebSocket = new ClientWebSocket();
                await retryPolicy.ExecuteAsync(async () => await _clientWebSocket.ConnectAsync(uri, _globalCancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(Connect), $"Unable to connect to BitMex after {attempts} attempts", ex);
            }
        }


        private static ArraySegment<byte> EncodeRequest(object request)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
        }

        public async Task SendRequest(object request)
        {
            try
            {
                var msg = EncodeRequest(request);
                await _clientWebSocket.SendAsync(msg, WebSocketMessageType.Text, true, _globalCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(Connect), "An exception occurred while sending request", ex);

                throw;
            }
        }


        public async Task<string> GetResponse()
        {
            using (var cts = new CancellationTokenSource(_responseTimeout))
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _globalCancellationTokenSource.Token))
            {

                var buffer = new byte[10000];
                var segment = new ArraySegment<byte>(buffer);
                var sb = new StringBuilder();
                var endOfMessage = false;
                while (!endOfMessage)
                {
                    var re = await _clientWebSocket.ReceiveAsync(buffer, linkedToken.Token);
                    sb.Append(DecodeText(segment.Array.Take(re.Count).ToArray()));
                    endOfMessage = re.EndOfMessage;
                }
                return sb.ToString();
            }
        }


        private static string DecodeText(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }

        public async Task Stop()
        {
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good bye", _globalCancellationTokenSource.Token);
                _globalCancellationTokenSource.Cancel();
            }
        }
    }
}
