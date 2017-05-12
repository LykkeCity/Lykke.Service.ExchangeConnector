using System.Threading.Tasks;
using TradingBot.OandaApi;
using TradingBot.OandaApi.ApiEndpoints;
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

            var result = await api.GetCandles("EUR_USD");
        }
    }
}
