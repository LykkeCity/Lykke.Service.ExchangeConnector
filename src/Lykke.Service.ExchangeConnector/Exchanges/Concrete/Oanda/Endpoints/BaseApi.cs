using System;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
{
    public abstract class BaseApi
    {
        protected readonly ApiClient ApiClient;

        protected BaseApi(ApiClient apiClient)
        {
            this.ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }
    }
}
