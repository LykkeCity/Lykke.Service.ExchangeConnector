using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Exchanges.Concrete.Kraken.Responses;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Concrete.Kraken.Endpoints
{
    /// <summary>
    /// Endpoint to Public data from API
    /// see https://www.kraken.com/help/api#public-market-data
    /// </summary>
    public class PublicData
    {
        private readonly string EndpointUrl = $"{Urls.ApiBase}/0/public";

        private readonly ApiClient apiClient;

        public PublicData(ApiClient apiClient)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task<ServerTime> GetServerTime()
        {
            return MakeGetRequestAsync<ServerTime>("Time");
        }

        public Task<Dictionary<string, AssetInfo>> GetAssetInfo()
        {
            return MakeGetRequestAsync<Dictionary<string, AssetInfo>>("Assets");
        }

        public Task<Dictionary<string, AssetPair>> GetAssetPairs()
        {
            return MakeGetRequestAsync<Dictionary<string, AssetPair>>("AssetPairs");
        }

        public Task<Dictionary<string, Ticker>> GetTickerInformation(params string[] pairs)
        {
            var queryString = string.Join(",", pairs);
            return MakeGetRequestAsync<Dictionary<string, Ticker>>($"Ticker?pair={queryString}");
        }
        
        public async Task<OhlcDataResult> GetOHLC(params string[] pairs)
        {
            var query = string.Join(",", pairs);
            var jObject = await MakeGetRequestAsync<JObject>($"OHLC?pair={query}");

            var res = new OhlcDataResult()
            {
                Last = (long)jObject["last"],
                Data = jObject.Properties().Where(x => x.Name != "last")
                    .ToDictionary(x => x.Name, x => x.Values().Select(v => new OhlcData(
                            v.Select(i => decimal.Parse(i.ToString(), CultureInfo.InvariantCulture)).ToArray()
                        )))
            };

            return res;
        }

        public Task<Dictionary<string, OrderBook>> GetOrderBook(string pair, int? count = null)
        {
            var url = $"Depth?pair={pair}";
            if (count.HasValue)
                url += $"&count={count}";

            return MakeGetRequestAsync<Dictionary<string, OrderBook>>(url);
        }

        private async Task<T> MakeGetRequestAsync<T>(string url)
        {
            var response = await apiClient.MakeGetRequestAsync<ResponseBase<T>>($"{EndpointUrl}/{url}");

            if (response.Error.Any())
            {
                throw new ApiException(string.Join("; ", response.Error));
            }

            return response.Result;
        }
    }
}
