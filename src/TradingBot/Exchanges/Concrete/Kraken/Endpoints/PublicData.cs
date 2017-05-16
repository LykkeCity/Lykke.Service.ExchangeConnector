using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
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

        public Task<ServerTimeResult> GetServerTime()
        {
            return MakeGetRequestAsync<ServerTimeResult>("Time");
        }

        public Task<Dictionary<string, AssetInfo>> GetAssetInfo()
        {
            return MakeGetRequestAsync<Dictionary<string, AssetInfo>>("Assets");
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
