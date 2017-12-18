using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models;
using TradingBot.Exchanges.Concrete.BitMEX;
using Xunit;

namespace TradingBot.Tests.BitMex
{
    public class BitMexApiClientTests
    {
        private BitMEXAPI _api;

        public BitMexApiClientTests()
        {
            var cred = new BitMexServiceClientCredentials("Your API key", "Your secret");
            _api = new BitMEXAPI(cred, new LoggingHandler(new HttpClientHandler()))
            {
                BaseUri = new Uri(@"https://testnet.bitmex.com/api/v1")
            };
        }

        [Fact]
        public async Task ShouldGetAllOrders()
        {
            var result = await _api.OrdergetOrdersAsync();
            Assert.NotNull(result);

            var orders = (IReadOnlyCollection<Order>)result;

            Assert.NotEmpty(orders);
        }

        [Fact]
        public async Task ShouldAddNewOrder()
        {
            var result = await _api.OrdernewAsync("XBTUSD", orderQty: 1, side: "Buy", ordType: "Market");

            Assert.NotNull(result);
            Assert.IsType<Order>(result);
        }

        [Fact]
        public async Task ShouldGetActiveOrders()
        {
            var filter = "{\"ordStatus\":\"New\"}";
            var result = await _api.OrdergetOrdersAsync(filter: filter);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyCollection<Order>>(result);
        }

        [Fact]
        public async Task ShouldGetMarginInfo()
        {
            var result = await _api.UsergetMarginWithHttpMessagesAsync("");

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyCollection<Order>>(result);
        }
    }

    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Request:");
            sb.AppendLine(request.ToString());
            if (request.Content != null)
            {
                sb.AppendLine(await request.Content.ReadAsStringAsync());
            }
            sb.AppendLine();

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            sb.AppendLine("Response:");
            sb.AppendLine(response.ToString());
            if (response.Content != null)
            {
                sb.AppendLine(await response.Content.ReadAsStringAsync());
            }
            sb.AppendLine();
            var re = sb.ToString();
            return response;
        }
    }
}
