using System;
using System.Threading.Tasks;
using TradingBot.OandaApi.Entities.Instruments;

namespace TradingBot.OandaApi.ApiEndpoints
{
    public class InstrumentsApi : BaseApi
    {
        public InstrumentsApi(ApiClient apiClient) : base(apiClient)
        {
        }

        public Task<CandlesResponse> GetCandles(string instrument)
        {
            return ApiClient.MakeRequestAsync<CandlesResponse>($"{OandaUrls.Instruments}/{instrument}/candles");
        }
    }
}
