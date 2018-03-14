using Common.Log;
using Newtonsoft.Json;
using Polly;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.ExternalExchangesApi.Shared
{
    public sealed class WebSocketTextMessenger : IMessenger<object, string>
    {
        private readonly string _endpointUrl;
        private readonly ILog _log;
        private ClientWebSocket _clientWebSocket;
#if DEBUG
        private readonly TimeSpan _responseTimeout = TimeSpan.FromSeconds(60);
#else
        private readonly TimeSpan _responseTimeout = TimeSpan.FromSeconds(10); 
#endif

        public WebSocketTextMessenger(string endpointUrl, ILog log)
        {

            _endpointUrl = endpointUrl;
            _log = log;
        }

        public void Dispose()
        {
            _clientWebSocket?.Dispose();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await _log.WriteInfoAsync(nameof(ConnectAsync), "Connecting to WebSocket", $"API endpoint {_endpointUrl}");
            var uri = new Uri(_endpointUrl);

            const int policyMaxAttempts = 20;
            const int retrySeconds = 3;
            var retryPolicy = Policy
                .Handle<Exception>(e => !cancellationToken.IsCancellationRequested)
                .WaitAndRetryAsync(policyMaxAttempts, attempt => TimeSpan.FromSeconds(retrySeconds), 
                (exception, timeSpan, attempt, context) =>
                {
                    if (attempt == 1)
                    {
                        _log.WriteWarningAsync(nameof(ConnectAsync), $"Unable to connect to {_endpointUrl}. Retry in {retrySeconds} sec.", exception.Message).GetAwaiter().GetResult();
                    }
                    if (attempt % policyMaxAttempts == 0)
                    {
                        _log.WriteErrorAsync(nameof(ConnectAsync), $"Unable to connect to {_endpointUrl} after {policyMaxAttempts} attempts", exception).GetAwaiter().GetResult();
                    }
                });

            await retryPolicy.ExecuteAsync(async () =>
            {
                _clientWebSocket = new ClientWebSocket();
                using (var connectionTimeoutCts = new CancellationTokenSource(_responseTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectionTimeoutCts.Token))
                {
                    await _clientWebSocket.ConnectAsync(uri, linkedCts.Token);
                }
                await _log.WriteInfoAsync(nameof(ConnectAsync), "Successfully connected to WebSocket",  $"API endpoint {_endpointUrl}");
            });
        }


        public async Task SendRequestAsync(object request, CancellationToken cancellationToken)
        {
            try
            {
                var msg = EncodeRequest(request);
                using (var connectionTimeoutCts = new CancellationTokenSource(_responseTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectionTimeoutCts.Token))
                {
                    await _clientWebSocket.SendAsync(msg, WebSocketMessageType.Text, true, linkedCts.Token);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteWarningAsync(nameof(ConnectAsync), request.ToString(), "An exception occurred while sending request", ex);
                throw;
            }
        }


        public async Task<string> GetResponseAsync(CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource(_responseTimeout))
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {

                var buffer = new byte[512];
                var segment = new ArraySegment<byte>(buffer);
                var sb = new StringBuilder();
                var endOfMessage = false;
                while (!endOfMessage)
                {
                    var re = await _clientWebSocket.ReceiveAsync(segment, linkedToken.Token);
                    if (re.MessageType == WebSocketMessageType.Close)
                    {
                        throw new OperationCanceledException("The remote host requested connection closing");
                    }
                    sb.Append(DecodeText(segment.Array.Take(re.Count).ToArray()));
                    endOfMessage = re.EndOfMessage;
                }
                return sb.ToString();
            }
        }



        private static ArraySegment<byte> EncodeRequest(object request)
        {
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)));
        }

        private static string DecodeText(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Good bye", cancellationToken);
            }
        }
    }
}
