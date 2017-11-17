using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.GDAX
{
    public class GdaxRestApiClientTests
    {
        private readonly GdaxRestApi _api;
        private readonly Guid _orderId = Guid.NewGuid();

        public GdaxRestApiClientTests()
        {
            var configuration = GdaxHelpers.GetGdaxConfiguration();
            _api = new GdaxRestApi(configuration.ApiKey, configuration.ApiSecret, configuration.PassPhrase)
            {
                BaseUri = new Uri(configuration.RestEndpointUrl),
                ConnectorUserAgent = configuration.UserAgent
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
            var newOrder = await _api.AddOrder("BTC-USD", 5, 100.01m, GdaxOrderSide.Buy, GdaxOrderType.Limit);

            var result = await _api.CancelOrder(newOrder.Id);
            
            Assert.True(result);
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
    }
}
