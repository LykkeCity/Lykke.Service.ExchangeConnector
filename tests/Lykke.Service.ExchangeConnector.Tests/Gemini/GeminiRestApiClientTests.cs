using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.Gemini.RestClient;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Gemini
{
    public class GeminiRestApiClientTests
    {
        private readonly GeminiRestApi _api;
        private readonly Guid _orderId = Guid.NewGuid();

        private const string _userAgent = "LykkeTest";
        private const string _apiKey = "";
        private const string _apiSecret = "";

        public GeminiRestApiClientTests()
        {
            _api = new GeminiRestApi(_apiKey, _apiSecret)
            {
                BaseUri = new Uri(GeminiRestApi.GeminiSandboxApiUrl),
                ConnectorUserAgent = _userAgent
            };
        }

        [Fact]
        public async Task GetOpenOrders()
        {
            var result = await _api.GetOpenOrders();
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyList<GeminiOrderResponse>>(result);
        }

        [Fact]
        public async Task AddNewMarketOrder()
        {
            var symbol = "BTC-USD";
            var amount = 2m;
            var price = 0;
            var orderSide = GeminiOrderSide.Buy;
            var orderType = GeminiOrderType.Market;

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
            var orderSide = GeminiOrderSide.Buy;
            var orderType = GeminiOrderType.Limit;

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
            var newOrder = await _api.AddOrder("BTC-USD", 5, 0.01m, GeminiOrderSide.Buy, GeminiOrderType.Limit);

            var result = await _api.CancelOrder(newOrder.Id);
            
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            var cancelledId = result.First();
            Assert.Equal(cancelledId, newOrder.Id);

            //var order = await _api.GetOrderStatus(newOrder.Id);
            //Assert.NotNull(order);
            //Assert.Equal(cancelledId, order.Id);
            //Assert.Equal("cancelled", order.Status);
        }

        /// <summary>
        /// Creates an order, gets its status and then cancels it
        /// </summary>
        [Fact]
        public async Task GetOrderStatus()
        {
            var newOrder = await _api.AddOrder("BTC-USD", 5, 0.01m, GeminiOrderSide.Buy, GeminiOrderType.Limit);

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
    }
}
