using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
            var result = await PublicData.GetServerTime(CancellationToken.None);

            Assert.True((result.Rfc1123 - DateTime.Now) < TimeSpan.FromMinutes(1));
            Assert.Equal(result.Rfc1123, result.FromUnixTime.ToLocalTime());
        }

        [Fact]
        public async Task GetAssetInfoTest()
        {
            var result = await PublicData.GetAssetInfo(CancellationToken.None);

            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetAssetPairs()
        {
            var result = await PublicData.GetAssetPairs(CancellationToken.None);

            var lines = result.Select(x => x.Value.ToString()).ToList();

            System.IO.File.WriteAllLines("pairs.txt", lines);

            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task GetTickerInformationTest()
        {
            var result = await PublicData.GetTickerInformation(CancellationToken.None, 
                "DASHEUR", "DASHUSD");

            Assert.True(result.Count == 2);
        }

        [Fact]
        public async Task GetOHLCTest()
        {
            var result = await PublicData.GetOHLC(CancellationToken.None, "DASHEUR");

            Assert.NotNull(result);
            Assert.True(result.Data.Values.First().Any());
        }

        [Fact]
        public async Task GetOrderBook()
        {
            var result = await PublicData.GetOrderBook(CancellationToken.None, "XXBTZUSD");

            Assert.True(result.Single().Value.Bids.Any());
        }

        [Fact]
        public async Task GetTrades()
        {
            var result = await PublicData.GetTrades(CancellationToken.None, "XXBTZUSD");

            Assert.Equal(1, result.Count);
            Assert.NotNull(result.Last);
        }
    }
}
