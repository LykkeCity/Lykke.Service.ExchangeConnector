using System;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.OandaApi;
using TradingBot.Exchanges.OandaApi.ApiEndpoints;
using TradingBot.Exchanges.OandaApi.Entities.Instruments;
using Xunit;

namespace TradingBot.Tests
{
    public class InstrumentsApiTests
    {
        private string GetToken => OandaAuth.Token;

        private InstrumentsApi CreateInstrumentsApi()
        {
            return new InstrumentsApi(new ApiClient(GetToken));
        }

        [Fact]
        public async Task GetCandles()
        {
            var api = CreateInstrumentsApi();
            var instrument = "EUR_USD";

            var result = await api.GetCandles(instrument, 
                from: DateTime.UtcNow.AddHours(-1), 
                to: null,
                granularity: CandlestickGranularity.S5);


            Assert.Equal(instrument, result.Instrument);
            Assert.True(result.Candles.Any());
        }
    }
}
