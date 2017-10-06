using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.AutorestClient;
using TradingBot.Exchanges.Concrete.AutorestClient.Models;
using TradingBot.Exchanges.Concrete.BitMEX;
using Xunit;

namespace TradingBot.Tests.BitMex
{
    public class ApiClientTests
    {
        private BitMEXAPI _api;

        public ApiClientTests()
        {
            var cred = new BitMexServiceClientCredentials("Your ApiKeyId", "SecretKey");
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
