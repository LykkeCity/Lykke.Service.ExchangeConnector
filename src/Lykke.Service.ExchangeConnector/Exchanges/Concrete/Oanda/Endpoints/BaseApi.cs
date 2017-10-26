using System;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
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
