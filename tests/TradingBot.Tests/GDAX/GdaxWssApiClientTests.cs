using System;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.WssClient;
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
            var skipTask = _api.SubscribeToPrivateUpdatesAsync(new[] { "btc-usd" }, cancellationToken);
            // TODO Handle events

            Task.Delay(5000);
            await _api.CloseConnectionAsync(cancellationToken);
        }
    }
}
