using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient;
using TradingBot.Exchanges.Concrete.Bitfinex;
using Xunit;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;

namespace TradingBot.Tests.Bitfinex
{
    public class BitfinexApiClientTests
    {
        private readonly BitfinexApi _api;

        public BitfinexApiClientTests()
        {
            var cred = new BitfinexServiceClientCredentials("Your API key", "Your secret");
            _api = new BitfinexApi(cred);
        }

        [Fact]
        public async Task ShouldGetAllOrders()
        {
            var result = await _api.GetActiveOrders();
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyList<Order>>(result);
        }

        [Fact]
        public async Task ShouldAddNewOrder()
        {
            var result = await _api.AddOrder("btcusd", 0.001m, 1, "buy", "market");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldCancelOrder()
        {
            var result = await _api.CancelOrder(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetOrderStatus()
        {
            var result = await _api.GetOrderStatus(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetBalances()
        {
            var result = await _api.GetBalances();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldGetGetMarginInformation()
        {
            var result = await _api.GetMarginInformation();
            Assert.NotNull(result);
        }


    }
}
