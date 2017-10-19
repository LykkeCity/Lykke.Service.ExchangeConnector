using System;
using System.Collections.Generic;
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
        private const string _apiKey = "1e127272cef41056e178817509caf26a";
        private const string _apiSecret = "Ajptk9vBwPfVQbhLCy2jsKZduHf3DGjXseK+7Gvqc2QIKaMZ1SrMG/U5Qz7SeXZbBR8Jr1GorQOZFVW59iQjyQ==";
        private const string _apiPassPhrase = "lcuu5q0u1i";

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
            Assert.IsAssignableFrom<IReadOnlyList<GdaxOrder>>(result);
        }

        [Fact]
        public async Task AddNewOrder()
        {
            var result = await _api.AddOrder("btc-usd", 0.001m, 1, "buy", "market");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CancelOrder()
        {
            var result = await _api.CancelOrder(_orderId);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetOrderStatus()
        {
            var result = await _api.GetOrderStatus(_orderId);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetBalances()
        {
            var result = await _api.GetBalances();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetGetMarginInformation()
        {
            var result = await _api.GetMarginInformation();
            Assert.NotNull(result);
        }
    }
}
