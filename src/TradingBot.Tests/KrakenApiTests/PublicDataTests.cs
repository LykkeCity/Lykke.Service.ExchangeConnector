using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Helpers;
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
            Assert.Equal(result.Rfc1123, result.FromUnixTime.ToLocalTime());
        }

        [Fact]
        public async Task GetAssetInfoTest()
        {
            var result = await PublicData.GetAssetInfo();

            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetAssetPairs()
        {
            var result = await PublicData.GetAssetPairs();

            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetTickerInformationTest()
        {
            var result = await PublicData.GetTickerInformation("DASHEUR", "DASHUSD");

            Assert.True(result.Count == 2);
        }

        [Fact]
        public async Task GetOHLCTest()
        {
            var result = await PublicData.GetOHLC("DASHEUR");

            Assert.NotNull(result);
            Assert.True(result.Data.Values.First().Any());
        }

        [Fact]
        public async Task GetOrderBook()
        {
            var result = await PublicData.GetOrderBook("XXBTZUSD");

            Assert.True(result.Single().Value.Bids.Any());
        }

        [Fact]
        public async Task GetTrades()
        {
            var result = await PublicData.GetTrades("XXBTZUSD");

            Assert.Equal(1, result.Count);
            Assert.NotNull(result.Last);
        }
    }
}
