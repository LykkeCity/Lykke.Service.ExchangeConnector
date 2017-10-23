using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.GDAX.Credentials;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient
{
    internal sealed class GdaxWebSocketsApi
    {
        public const string GdaxPublicWssApiUrl = @"wss://ws-feed.gdax.com";
        public const string GdaxSandboxWssApiUrl = @"wss://ws-feed-public.sandbox.gdax.com";

        private readonly GdaxCredentialsFactory _credentialsFactory;

        /// <summary>
        /// Base GDAX WebSockets Uri
        /// </summary>
        public Uri BaseUri { get; set; }

        public GdaxWebSocketsApi(string apiKey, string apiSecret, string passPhrase)
        {
            _credentialsFactory = new GdaxCredentialsFactory(apiKey, apiSecret, passPhrase);

            BaseUri = new Uri(GdaxPublicWssApiUrl);
        }

        public async Task SubscribeAsync(CancellationToken cancellationToken)
        {
            var webSocketClient = new ClientWebSocket();
            await webSocketClient.ConnectAsync(BaseUri, cancellationToken).ConfigureAwait(false);
            
            if (webSocketClient.State == WebSocketState.Open)
            {
                // TODO: Send connected event

                var requestString = JsonConvert.SerializeObject(new
                {
                    type = "subscribe",
                    // TODO: key,secred,passphrase,profileid,userid
                    //product_ids = ProductIds,  // TODO
                    //channels = new string[] { "ticker" }
                });

                var requestBytes = UTF8Encoding.UTF8.GetBytes(requestString);
                var subscribeRequest = new ArraySegment<byte>(requestBytes);
                var sendCancellationToken = new CancellationToken();
                await webSocketClient.SendAsync(subscribeRequest, WebSocketMessageType.Text, true, sendCancellationToken).ConfigureAwait(false);

                // TODO: Send subscribed event

                while (webSocketClient.State == WebSocketState.Open)
                {
                    var receiveCancellationToken = new CancellationToken();
                    using (var stream = new MemoryStream(1024))
                    {
                        var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 8]);
                        WebSocketReceiveResult webSocketReceiveResult;
                        do
                        {
                            webSocketReceiveResult = await webSocketClient.ReceiveAsync(receiveBuffer, receiveCancellationToken).ConfigureAwait(false);
                            await stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, receiveBuffer.Count);
                        } while (!webSocketReceiveResult.EndOfMessage);

                        var message = stream.ToArray();
                        HandleWssMessage(Encoding.ASCII.GetString(message, 0, message.Length));
                    }
                }
            }
        }

        private void HandleWssMessage(string message)
        {
            throw new NotImplementedException();
        }
    }
}
