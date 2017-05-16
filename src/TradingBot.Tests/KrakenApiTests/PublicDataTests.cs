using System;
using System.Net.Http;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using Xunit;

namespace TradingBot.Tests.KrakenApiTests
{
    public class PublicDataTests
    {
        private PublicData PublicData => new PublicData(new ApiClient(new HttpClient()));

        [Fact]
        public async Task GetServerTimeTest()
        {
            var result = await PublicData.GetServerTime();

            Assert.True((result.Rfc1123 - DateTime.Now) < TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAssetInfoTest()
        {
            var result = await PublicData.GetAssetInfo();

            Assert.True(result.Count > 0);
        }
    }
}
