using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradingBot.Exchanges.Concrete.GDAX.Credentials;
using TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Concrete.GDAX.WssClient
{
    internal class GdaxWebSocketApi: IDisposable
    {
        public const string GdaxPublicWssApiUrl = @"wss://ws-feed.gdax.com";
        public const string GdaxSandboxWssApiUrl = @"wss://ws-feed-public.sandbox.gdax.com";
        private const string _selfVerifyUrl = @"/users/self/verify";

        private readonly GdaxCredentialsFactory _credentialsFactory;
        private ClientWebSocket _clientWebSocket;

        /// <summary>
        /// Raised on established WebSocket connection with the server
        /// </summary>
        public AsyncEvent<Uri> Connected;

        /// <summary>
        /// Raised when WebSocket connection is lost
        /// </summary>
        public AsyncEvent<Uri> Disconnected;

        /// <summary>
        /// Raised on successful subscription
        /// </summary>
        public AsyncEvent<string> Subscribed;

        /// <summary>
        /// Raised when a valid order has been received and is now active. This message 
        /// is emitted for every single valid order as soon as the matching engine receives 
        /// it whether it fills immediately or not.
        /// </summary>
        public AsyncEvent<GdaxWssOrderReceived> OrderReceived;

        /// <summary>
        /// Raised when the order is now open on the order book. This message will only be 
        /// sent for orders which are not fully filled immediately. RemainingSize will 
        /// indicate how much of the order is unfilled and going on the book
        /// </summary>
        public AsyncEvent<GdaxWssOrderOpen> OrderOpened;

        /// <summary>
        /// Raised when the order is no longer on the order book. Sent for all orders for which there 
        /// was a received message. This message can result from an order being canceled or filled. 
        /// There will be no more messages for this OrderId after a done message. RemainingSize indicates 
        /// how much of the order went unfilled; this will be 0 for filled orders.
        /// Market orders will not have a remaining_size or price field as they are never on the 
        /// open order book at a given price.
        /// </summary>
        public AsyncEvent<GdaxWssOrderDone> OrderDone;

        /// <summary>
        /// Raised when a trade occurred between two orders. The aggressor or taker order is the one 
        /// executing immediately after being received and the maker order is a resting order on the book. 
        /// The side field indicates the maker order side. If the side is sell this indicates the maker 
        /// was a sell order and the match is considered an up-tick. A buy side match is a down-tick.
        /// </summary>
        public AsyncEvent<GdaxWssOrderMatch> OrderMatched;

        /// <summary>
        /// Raised when an order has changed. This is the result of self-trade prevention adjusting the 
        /// order size or available funds. Orders can only decrease in size or funds. Change messages are 
        /// sent anytime an order changes in size; this includes resting orders (open) as well as received 
        /// but not yet open. Change messages are also sent when a new market order goes through self trade 
        /// prevention and the funds for the market order have changed.
        /// </summary>
        public AsyncEvent<GdaxWssOrderChange> OrderChanged;

        /// <summary>
        /// The ticker provides real-time price updates every time a match happens. It batches updates 
        /// in case of cascading matches, greatly reducing bandwidth requirements.
        /// </summary>
        public AsyncEvent<GdaxWssTicker> Ticker;

        /// <summary>
        /// Most failure cases will cause an error message (a message with the type "error") to be emitted. 
        /// This can be helpful for implementing a client or debugging issues.
        /// </summary>
        public AsyncEvent<GdaxWssError> Error;

        /// <summary>
        /// Base GDAX WebSockets Uri
        /// </summary>
        public Uri BaseUri { get; set; }

        public GdaxWebSocketApi(string apiKey, string apiSecret, string passPhrase)
        {
            _credentialsFactory = new GdaxCredentialsFactory(apiKey, apiSecret, passPhrase);

            BaseUri = new Uri(GdaxPublicWssApiUrl);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            _clientWebSocket = new ClientWebSocket();
            await _clientWebSocket.ConnectAsync(BaseUri, cancellationToken).ConfigureAwait(false);

            if (_clientWebSocket.State != WebSocketState.Open)
                throw new ApiException($"Could not establish WebSockets connection to {BaseUri}");

            await Connected.NullableInvokeAsync(this, BaseUri);
        }

        public async Task SubscribeToPrivateUpdatesAsync(IReadOnlyCollection<string> productIds, CancellationToken cancellationToken)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                throw new ApiException($"Could not subscribe to {BaseUri} because no connection is established.");

            var credentials = _credentialsFactory.GenerateCredentials(HttpMethod.Get, 
                new Uri(BaseUri, _selfVerifyUrl), string.Empty);
            var requestString = JsonConvert.SerializeObject(new
            {
                type = "subscribe",
                signature = credentials.Signature,
                key = credentials.ApiKey,
                passphrase = credentials.PassPhrase,
                timestamp = credentials.UnixTimestampString,
                channels = new[] {
                    new { name = "ticker", product_ids = productIds },
                    new { name = "user", product_ids = productIds },
                }
            });

            await SubscribeImplAsync(cancellationToken, requestString);
        }

        public async Task SubscribeToFullUpdatesAsync(IReadOnlyCollection<string> productIds, CancellationToken cancellationToken)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
                throw new ApiException($"Could not subscribe to {BaseUri} because no connection is established.");

            var credentials = _credentialsFactory.GenerateCredentials(HttpMethod.Get,
                new Uri(BaseUri, _selfVerifyUrl), string.Empty);
            var requestString = JsonConvert.SerializeObject(new
            {
                type = "subscribe",
                signature = credentials.Signature,
                key = credentials.ApiKey,
                passphrase = credentials.PassPhrase,
                timestamp = credentials.UnixTimestampString,
                channels = new[] {
                    new { name = "ticker", product_ids = productIds },
                    new { name = "full", product_ids = productIds },
                }
            });

            await SubscribeImplAsync(cancellationToken, requestString);
        }

        private async Task SubscribeImplAsync(CancellationToken cancellationToken, string requestString)
        {
            await _clientWebSocket.SendAsync(StringToArraySegment(requestString), WebSocketMessageType.Text,
                true, cancellationToken).ConfigureAwait(false);

            await Subscribed.NullableInvokeAsync(this, requestString);

            await ListenToMessagesAsync(_clientWebSocket, cancellationToken);
        }

        private async Task ListenToMessagesAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                using (var stream = new MemoryStream(1024))
                {
                    var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 8]);
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await webSocket.ReceiveAsync(receiveBuffer,
                            cancellationToken).ConfigureAwait(false);
                        await stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, receiveBuffer.Count, 
                            cancellationToken);
                    } while (!receiveResult.EndOfMessage);

                    var messageBytes = stream.ToArray();
                    var jsonMessage = Encoding.UTF8.GetString(messageBytes, 0, messageBytes.Length);
                    await HandleWebSocketMessageAsync(jsonMessage);
                }
            }
        }

        public async Task CloseConnectionAsync(CancellationToken cancellationToken)
        {
            if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", cancellationToken);
                await Disconnected.NullableInvokeAsync(this, BaseUri);
            }
        }

        private ArraySegment<byte> StringToArraySegment(string message)
        {
            var messageBytes = UTF8Encoding.UTF8.GetBytes(message);
            var messageArraySegment = new ArraySegment<byte>(messageBytes);
            return messageArraySegment;
        }

        private async Task HandleWebSocketMessageAsync(string jsonMessage)
        {
            var jToken = JToken.Parse(jsonMessage);
            var type = jToken["type"]?.Value<string>();

            switch (type)
            {
                case "received":
                    var orderReceived = JsonConvert.DeserializeObject<GdaxWssOrderReceived>(jsonMessage);
                    await OrderReceived.NullableInvokeAsync(this, orderReceived);
                    break;
                case "open":
                    var orderOpen = JsonConvert.DeserializeObject<GdaxWssOrderOpen>(jsonMessage);
                    await OrderOpened.NullableInvokeAsync(this, orderOpen);
                    break;
                case "done":
                    var orderDone = JsonConvert.DeserializeObject<GdaxWssOrderDone>(jsonMessage);
                    await OrderDone.NullableInvokeAsync(this, orderDone);
                    break;
                case "match":
                    var orderMatch = JsonConvert.DeserializeObject<GdaxWssOrderMatch>(jsonMessage);
                    await OrderMatched.NullableInvokeAsync(this, orderMatch);
                    break;
                case "change":
                    var orderChange = JsonConvert.DeserializeObject<GdaxWssOrderChange>(jsonMessage);
                    await OrderChanged.NullableInvokeAsync(this, orderChange);
                    break;
                case "ticker":
                    var tickerDetails = JsonConvert.DeserializeObject<GdaxWssTicker>(jsonMessage);
                    await Ticker.NullableInvokeAsync(this, tickerDetails);
                    break;
                case "error":
                    var error = JsonConvert.DeserializeObject<GdaxWssError>(jsonMessage);
                    await Error.InvokeAsync(this, error);
                    break;
                default:
                    // Clients are expected to ignore messages they do not support.
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GdaxWebSocketApi()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (_clientWebSocket != null)
                {
                    _clientWebSocket.Abort();
                    _clientWebSocket.Dispose();
                    _clientWebSocket = null;
                }
            }
        }
    }
}
