using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;
using Xunit;

namespace TradingBot.Tests.GDAX
{
    public class GdaxApiClientTests
    {
        private readonly GdaxApi _api;
        private readonly Guid _orderId = Guid.NewGuid();

        private const string _userAgent = "LykkeTest";
        private const string _apiKey = "143f281de7ab32f6269eb0dc9aa14aeb";
        private const string _apiSecret = "H/bVM/bBNcLDAToPmloL1IJe0KKW0XjLk4HA/UUrO/e/91tsx5Y56BsG6hgGaReV1MIShv1LDUNLCJ99wgDk0Q==";
        private const string _apiPassPhrase = "prulog9byo9";

        public GdaxApiClientTests()
        {
            var cred = new GdaxServiceClientCredentials(_apiKey, _apiSecret, _apiPassPhrase);
            _api = new GdaxApi(cred)
            {
                BaseUri = new Uri(GdaxApi.GdaxSandboxApiUrl),
                ConnectorUserAgent = _userAgent
            };
        }

        [Fact]
        public async Task GetOpenOrders()
        {
            var result = await _api.GetOpenOrders();
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyList<GdaxOrderResponse>>(result);
        }

        [Fact]
        public async Task AddNewMarketOrder()
        {
            var symbol = "BTC-USD";
            var amount = 2m;
            var price = 0;
            var orderSide = GdaxOrderSide.Buy;
            var orderType = GdaxOrderType.Market;

            var result = await _api.AddOrder(symbol, amount, price, orderSide, orderType);

            Assert.NotNull(result);
            Assert.Equal(symbol, result.ProductId);
            Assert.Equal(amount, result.Size);
            Assert.Equal(price, result.Price);
            Assert.Equal(orderSide, result.Side);
            Assert.Equal(orderType, result.OrderType);
        }

        [Fact]
        public async Task AddNewLimitOrder()
        {
            var symbol = "BTC-USD";
            var amount = 1.15m;
            var price = 1;
            var orderSide = GdaxOrderSide.Buy;
            var orderType = GdaxOrderType.Limit;

            var result = await _api.AddOrder(symbol, amount, price, orderSide, orderType);
            
            Assert.NotNull(result);
            Assert.Equal(symbol, result.ProductId);
            Assert.Equal(amount, result.Size);
            Assert.Equal(price, result.Price);
            Assert.Equal(orderSide, result.Side);
            Assert.Equal(orderType, result.OrderType);
        }

        /// <summary>
        /// Creates a new order and then cancels it
        /// </summary>
        [Fact]
        public async Task AddAndCancelOrder()
        {
            var newOrder = await _api.AddOrder("BTC-USD", 5, 0.01m, GdaxOrderSide.Buy, GdaxOrderType.Limit);
            //var newOrder2 = await _api.AddOrder("BTC-USD", 10, 0.01m, GdaxOrderSide.Buy, GdaxOrderType.Limit);
            
            var result = await _api.CancelOrder(newOrder.Id);

            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal(result.First(), newOrder.Id);
        }

        /// <summary>
        /// Creates an order, gets its status and then cancels it
        /// </summary>
        [Fact]
        public async Task GetOrderStatus()
        {
            var newOrder = await _api.AddOrder("BTC-USD", 5, 0.01m, GdaxOrderSide.Buy, GdaxOrderType.Limit);

            var result = await _api.GetOrderStatus(newOrder.Id);

            await _api.CancelOrder(newOrder.Id);

            Assert.NotNull(result);
            Assert.Equal(result.Id, newOrder.Id);
            Assert.Equal(result.ProductId, newOrder.ProductId);
            Assert.Equal(result.Size, newOrder.Size);
            Assert.Equal(result.Price, newOrder.Price);
            Assert.Equal(result.Side, newOrder.Side);
            Assert.Equal(result.OrderType, newOrder.OrderType);
        }

        [Fact]
        public async Task GetBalances()
        {
            var result = await _api.GetBalances();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetGetMarginInformation()
        {
            var result = await _api.GetMarginInformation();
            Assert.NotNull(result);
        }
    }
}
