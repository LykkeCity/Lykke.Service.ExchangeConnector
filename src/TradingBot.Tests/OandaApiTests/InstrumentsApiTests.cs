using System;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda;
using TradingBot.Exchanges.Concrete.Oanda.Endpoints;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Instruments;
using Xunit;

namespace TradingBot.Tests.OandaApiTests
{
    public class InstrumentsApiTests
    {
        private string GetToken => OandaAuth.Token;

        private Instruments CreateInstrumentsApi()
        {
            return new Instruments(new ApiClient(OandaHttpClient.CreateHttpClient(GetToken)));
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
