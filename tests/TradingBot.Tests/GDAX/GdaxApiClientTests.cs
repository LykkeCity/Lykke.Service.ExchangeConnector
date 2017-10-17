using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using Xunit;
using Order = TradingBot.Exchanges.Concrete.AutorestClient.Models.Order;

namespace TradingBot.Tests.GDAX
{
    public class GdaxApiClientTests
    {
        private readonly GdaxApi _api;

        private const string ApiKey = "YourAPIkey";
        private const string ApiSecret = "YourAPIsecret";
        private const string ApiPassPhrase = "YourAPIpassphrase";

        public GdaxApiClientTests()
        {
            var cred = new GdaxServiceClientCredentials(ApiKey, ApiSecret, ApiPassPhrase);
            _api = new GdaxApi(cred);
        }

        [Fact]
        public async Task GetAllOrders()
        {
            var result = await _api.GetActiveOrders();
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyList<Order>>(result);
        }

        [Fact]
        public async Task AddNewOrder()
        {
            var result = await _api.AddOrder("btcusd", 0.001m, 1, "buy", "market");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CancelOrder()
        {
            var result = await _api.CancelOrder(1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetOrderStatus()
        {
            var result = await _api.GetOrderStatus(1);
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
