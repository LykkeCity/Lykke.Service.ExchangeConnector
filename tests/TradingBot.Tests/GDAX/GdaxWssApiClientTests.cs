using System;
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

        private const string _apiKey = "143f281de7ab32f6269eb0dc9aa14aeb";
        private const string _apiSecret = "H/bVM/bBNcLDAToPmloL1IJe0KKW0XjLk4HA/UUrO/e/91tsx5Y56BsG6hgGaReV1MIShv1LDUNLCJ99wgDk0Q==";
        private const string _apiPassPhrase = "prulog9byo9";
        private const string _btcUsd = "BTC-USD";

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
            var tcsOrderReceived = new TaskCompletionSource<GdaxWssOrderReceived>();
            var tcsOrderMarkedAsDone = new TaskCompletionSource<GdaxWssOrderDone>();
            _api.OrderReceived += (sender, order) => { tcsOrderReceived.TrySetResult(order); };
            _api.OrderDone += (sender, order) => { tcsOrderMarkedAsDone.TrySetResult(order); };

            // Connect and subscribe to web socket events
            await _api.ConnectAsync(cancellationToken);
            try
            {
                var skipTask = _api.SubscribeToPrivateUpdatesAsync(new[] { _btcUsd }, cancellationToken);

                // Raise some events
                newOrder = await CreateAndCancelOrderAsync();

                // Wait maximum 5 seconds the received and done events to be received
                var ordersTask = Task.WhenAll(tcsOrderReceived.Task, tcsOrderMarkedAsDone.Task);
                var delayTask = Task.Delay(5000);
                await Task.WhenAny(ordersTask, delayTask);
            }
            finally
            {
                await _api.CloseConnectionAsync(cancellationToken);
            }

            // Check if events were received successfuly
            Assert.NotNull(tcsOrderReceived.Task);
            Assert.True(tcsOrderReceived.Task.IsCompletedSuccessfully);
            var orderReceived = tcsOrderReceived.Task.Result;
            Assert.Equal(newOrder.Id, orderReceived.OrderId);
            Assert.Equal(newOrder.Price, orderReceived.Price);
            Assert.Equal(newOrder.ProductId, orderReceived.ProductId);
            Assert.Equal(newOrder.Side, orderReceived.Side);
            Assert.Equal(newOrder.Size, orderReceived.Size);
            Assert.Equal(newOrder.OrderType, orderReceived.Type);

            Assert.NotNull(tcsOrderMarkedAsDone.Task);
            Assert.True(tcsOrderMarkedAsDone.Task.IsCompletedSuccessfully);
            var orderMarkedAsDone = tcsOrderMarkedAsDone.Task.Result;
            Assert.NotNull(orderMarkedAsDone);
            Assert.Equal(newOrder.Id, orderMarkedAsDone.OrderId);
            Assert.Equal(newOrder.Id, orderMarkedAsDone.OrderId);
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
    }
}
