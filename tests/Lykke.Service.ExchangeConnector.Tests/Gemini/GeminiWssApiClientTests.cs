using System;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.Gemini.RestClient;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;
using TradingBot.Exchanges.Concrete.Gemini.WssClient;
using TradingBot.Exchanges.Concrete.Gemini.WssClient.Entities;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Gemini
{
    public class GeminiWssApiClientTests
    {
        private readonly GeminiWebSocketApi _api;

        private const string _apiKey = "";
        private const string _apiSecret = "";
        private const string _apiPassPhrase = "";
        private const string _btcUsd = "BTC-USD";
        private const string _orderDoneTypeName = "done";
        private const string _orderCanceledReason = "canceled";

        public GeminiWssApiClientTests()
        {
            _api = new GeminiWebSocketApi(_apiKey, _apiSecret)
            {
                BaseUri = new Uri(GeminiWebSocketApi.GeminiSandboxWssApiUrl)
            };
        }

        [Fact]
        public async Task ConnectAndDisconnect()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            await _api.ConnectAsync(cancellationToken);
            await _api.CloseConnectionAsync(cancellationToken);
        }

        [Fact]
        public async Task Subscribe()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            await _api.ConnectAsync(cancellationToken);
            var skipTask = _api.SubscribeToPrivateUpdatesAsync(new[] { _btcUsd }, cancellationToken);
            await _api.CloseConnectionAsync(cancellationToken);
        }

        [Fact]
        public async Task SubscribeAndHandleOrderEvents()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            GeminiOrderResponse newOrder;
            var tcsOrderReceived = new TaskCompletionSource<GeminiWssOrderReceived>();
            var tcsOrderOpened = new TaskCompletionSource<GeminiWssOrderOpen>();
            var tcsOrderMarkedAsDone = new TaskCompletionSource<GeminiWssOrderDone>();
            _api.OrderReceived += (sender, order) => { tcsOrderReceived.SetResult(order); };
            _api.OrderOpened += (sender, order) => { tcsOrderOpened.SetResult(order); };
            _api.OrderDone += (sender, order) => { tcsOrderMarkedAsDone.SetResult(order); };

            // Connect and subscribe to web socket events
            await _api.ConnectAsync(cancellationToken);
            try
            {
                // Subscribe
                var subscribed = await SubscribeAsync(10000, cancellationToken);
                Assert.True(subscribed);

                // Raise some events
                newOrder = await CreateAndCancelOrderAsync();

                // Wait maximum n seconds the received and done events to be received
                await WhenAllTaskAreDone(10000, tcsOrderReceived.Task, tcsOrderMarkedAsDone.Task);
            }
            finally
            {
                await _api.CloseConnectionAsync(cancellationToken);
            }

            // Check if events were received successfuly
            // Order received event
            Assert.NotNull(tcsOrderReceived.Task);
            Assert.True(tcsOrderReceived.Task.IsCompletedSuccessfully);
            var orderReceived = tcsOrderReceived.Task.Result;
            Assert.Equal(newOrder.Id, orderReceived.OrderId);
            Assert.Equal(newOrder.Price, orderReceived.Price);
            Assert.Equal(newOrder.ProductId, orderReceived.ProductId);
            Assert.Equal(newOrder.Side, orderReceived.Side);
            Assert.Equal(newOrder.Size, orderReceived.Size);

            // Order opened event
            Assert.NotNull(tcsOrderOpened.Task);
            Assert.True(tcsOrderOpened.Task.IsCompletedSuccessfully);
            var orderOpened = tcsOrderOpened.Task.Result;
            Assert.Equal(newOrder.Id, orderOpened.OrderId);
            Assert.Equal(newOrder.Price, orderOpened.Price);
            Assert.Equal(newOrder.ProductId, orderOpened.ProductId);
            Assert.Equal(newOrder.Side, orderOpened.Side);
            Assert.Equal(newOrder.Size, orderOpened.RemainingSize);

            // Order canceled event
            Assert.NotNull(tcsOrderMarkedAsDone.Task);
            Assert.True(tcsOrderMarkedAsDone.Task.IsCompletedSuccessfully);
            var orderMarkedAsDone = tcsOrderMarkedAsDone.Task.Result;
            Assert.NotNull(orderMarkedAsDone);
            Assert.Equal(newOrder.Id, orderMarkedAsDone.OrderId);
            Assert.Equal(_orderDoneTypeName, orderMarkedAsDone.Type);
            Assert.Equal(_orderCanceledReason, orderMarkedAsDone.Reason);
        }

        [Fact]
        public async Task SubscribeAndHandleTickerEvents()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            var tcsTicker = new TaskCompletionSource<GeminiWssTicker>();
            _api.Ticker += (sender, ticker) => { tcsTicker.SetResult(ticker); };

            // Connect and subscribe to web socket events
            await _api.ConnectAsync(cancellationToken);
            try
            {
                // Subscribe
                var subscribed = await SubscribeAsync(10000, cancellationToken);
                Assert.True(subscribed);

                // Wait maximum n seconds the received and done events to be received
                await WhenAllTaskAreDone(10000, tcsTicker.Task);
            }
            finally
            {
                await _api.CloseConnectionAsync(cancellationToken);
            }

            // Check if events were received successfuly
            // Ticker event
            Assert.NotNull(tcsTicker.Task);
            Assert.True(tcsTicker.Task.IsCompletedSuccessfully);
            var tick = tcsTicker.Task.Result;
            Assert.Equal(_btcUsd, tick.ProductId);
        }

        private async Task<bool> SubscribeAsync(int timeoutMs, CancellationToken cancellationToken)
        {
            var tcsSubscribed = new TaskCompletionSource<string>();
            _api.Subscribed += (sender, message) => { tcsSubscribed.SetResult(message); };

            // Subscribe
            var skipTask = _api.SubscribeToPrivateUpdatesAsync(new[] { _btcUsd }, cancellationToken);
            await WhenAllTaskAreDone(10000, tcsSubscribed.Task);  // Wait max n milliseconds for subscription

            return tcsSubscribed != null && tcsSubscribed.Task.IsCompletedSuccessfully;
        }

        private GeminiRestApi CreateRestApi()
        {
            return new GeminiRestApi(_apiKey, _apiSecret)
            {
                BaseUri = new Uri(GeminiRestApi.GeminiSandboxApiUrl)
            };
        }

        private async Task<GeminiOrderResponse> CreateAndCancelOrderAsync()
        {
            var restApiTests = CreateRestApi();
            var newOrder = await restApiTests.AddOrder(_btcUsd, 5, 0.01m, GeminiOrderSide.Buy, GeminiOrderType.Limit);
            await restApiTests.CancelOrder(newOrder.Id);

            return newOrder;
        }

        private async Task WhenAllTaskAreDone(int timeoutMs, params Task[] tasks)
        {
            await Task.WhenAny(Task.Delay(timeoutMs), Task.WhenAll(tasks));
        }
    }
}
