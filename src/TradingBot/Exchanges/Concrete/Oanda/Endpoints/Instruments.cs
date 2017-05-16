using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Instruments;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
{
    public class Instruments : BaseApi
    {
        public Instruments(ApiClient apiClient) : base(apiClient)
        {
        }

        public Task<CandlesResponse> GetCandles(string instrument, 
            DateTime from, 
            DateTime? to,
            CandlestickGranularity granularity)
        {
            var apiUrl = $"{OandaUrls.Instruments}/{instrument}/candles";

            var queryParams = new Dictionary<string, string>();
            queryParams.Add("from", FormatDateTimeToQuery(from));

            if (to.HasValue)
            {
                queryParams.Add("to", FormatDateTimeToQuery(to.Value));
            }

            queryParams.Add("granularity", granularity.ToString());

            var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"));

            return ApiClient.MakeGetRequestAsync<CandlesResponse>($"{apiUrl}?{queryString}");
        }

        private string FormatDateTimeToQuery(DateTime time)
        {
            return (time.ToString("yyyy-MM-ddTHH:mm:ss") + ".000000000Z").Replace(":", "%3A");
        }
    }
}
