using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using TradingBot.Exchanges.Concrete.GDAX.WssClient;
using TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities;
using Xunit;

namespace TradingBot.Tests.GDAX
{
    public class GdaxWssApiClientTests
    {
        private readonly GdaxWebSocketApi _api;
        private readonly Guid _orderId = Guid.NewGuid();

        private const string _apiKey = "";
        private const string _apiSecret = "";
        private const string _apiPassPhrase = "";
        private const string _btcUsd = "BTC-USD";
        private const string _orderDoneTypeName = "done";
        private const string _orderCanceledReason = "canceled";

        public GdaxWssApiClientTests()
        {
            _api = new GdaxWebSocketApi(_apiKey, _apiSecret, _apiPassPhrase)
            {
                BaseUri = new Uri(GdaxWebSocketApi.GdaxSandboxWssApiUrl)
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
        public async Task SubscribeAndHandleEvents()
        {
            var cancellationToken = new CancellationTokenSource().Token;

            GdaxOrderResponse newOrder;
            var tcsSubscribed = new TaskCompletionSource<string>();
            var tcsOrderReceived = new TaskCompletionSource<GdaxWssOrderReceived>();
            var tcsOrderMarkedAsDone = new TaskCompletionSource<GdaxWssOrderDone>();
            _api.Subscribed += (sender, message) => { tcsSubscribed.SetResult(message); };
            _api.OrderReceived += (sender, order) => { tcsOrderReceived.SetResult(order); };
            _api.OrderDone += (sender, order) => { tcsOrderMarkedAsDone.SetResult(order); };

            // Connect and subscribe to web socket events
            await _api.ConnectAsync(cancellationToken);
            try
            {
                // Subscribe
                var skipTask = _api.SubscribeToPrivateUpdatesAsync(new[] { _btcUsd }, cancellationToken);
                await WhenAllTaskAreDone(10000, tcsSubscribed.Task);  // Wait n seconds for subscription

                // Raise some events
                newOrder = await CreateAndCancelOrderAsync();

                // Wait maximum n seconds the received and done events to be received
                await WhenAllTaskAreDone(5000, tcsOrderReceived.Task, tcsOrderMarkedAsDone.Task);
            }
            finally
            {
                await _api.CloseConnectionAsync(cancellationToken);
            }

            // Check if events were received successfuly
            Assert.NotNull(tcsSubscribed);
            Assert.True(tcsSubscribed.Task.IsCompletedSuccessfully);
            Assert.NotNull(tcsOrderReceived.Task);
            Assert.True(tcsOrderReceived.Task.IsCompletedSuccessfully);
            var orderReceived = tcsOrderReceived.Task.Result;
            Assert.Equal(newOrder.Id, orderReceived.OrderId);
            Assert.Equal(newOrder.Price, orderReceived.Price);
            Assert.Equal(newOrder.ProductId, orderReceived.ProductId);
            Assert.Equal(newOrder.Side, orderReceived.Side);
            Assert.Equal(newOrder.Size, orderReceived.Size);

            Assert.NotNull(tcsOrderMarkedAsDone.Task);
            Assert.True(tcsOrderMarkedAsDone.Task.IsCompletedSuccessfully);
            var orderMarkedAsDone = tcsOrderMarkedAsDone.Task.Result;
            Assert.NotNull(orderMarkedAsDone);
            Assert.Equal(newOrder.Id, orderMarkedAsDone.OrderId);
            Assert.Equal(_orderDoneTypeName, orderMarkedAsDone.Type);
            Assert.Equal(_orderCanceledReason, orderMarkedAsDone.Reason);
        }

        private GdaxRestApi CreateRestApi()
        {
            return new GdaxRestApi(_apiKey, _apiSecret, _apiPassPhrase)
            {
                BaseUri = new Uri(GdaxRestApi.GdaxSandboxApiUrl)
            };
        }

        private async Task<GdaxOrderResponse> CreateAndCancelOrderAsync()
        {
            var restApiTests = CreateRestApi();
            var newOrder = await restApiTests.AddOrder(_btcUsd, 5, 0.01m, GdaxOrderSide.Buy, GdaxOrderType.Limit);
            await restApiTests.CancelOrder(newOrder.Id);

            return newOrder;
        }

        private async Task WhenAllTaskAreDone(int timeoutMs, params Task[] tasks)
        {
            await Task.WhenAny(Task.Delay(timeoutMs), Task.WhenAll(tasks));
        }
    }
}
