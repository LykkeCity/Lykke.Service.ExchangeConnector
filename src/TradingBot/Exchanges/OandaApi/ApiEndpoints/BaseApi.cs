using System;

namespace TradingBot.Exchanges.OandaApi.ApiEndpoints
{
    public abstract class BaseApi
    {
        protected readonly ApiClient ApiClient;

        public BaseApi(ApiClient apiClient)
        {
            this.ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }
    }
}
