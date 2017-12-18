using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.Oanda.Entities.Accounts;

namespace TradingBot.Exchanges.Concrete.Oanda.Endpoints
{
    public class Accounts : BaseApi
    {
        public Accounts(ApiClient apiClient) : base(apiClient)
        {
        }

        public Task<AccountsList> GetAccounts()
        {
            return GetAccounts(new CancellationToken());
        }

        public Task<AccountsList> GetAccounts(CancellationToken cancellationToken)
        {
            return ApiClient.MakeGetRequestAsync<AccountsList>(OandaUrls.Accounts, cancellationToken);
        }

        public Task<AccountDetails> GetAccountDetails(string accountId)
        {
            return GetAccountDetails(new CancellationToken(), accountId);
        }

        public Task<AccountDetails> GetAccountDetails(CancellationToken cancellationToken, string accountId)
        {
            return ApiClient.MakeGetRequestAsync<AccountDetails>($"{OandaUrls.Accounts}/{accountId}", cancellationToken);
        }

        public Task<AccountInstrumentsResponse> GetAccountInstruments(string accountId)
        {
            return GetAccountInstruments(new CancellationToken(), accountId);
        }

        public Task<AccountInstrumentsResponse> GetAccountInstruments(CancellationToken cancellationToken, string accountId)
        {
            return ApiClient.MakeGetRequestAsync<AccountInstrumentsResponse>($"{OandaUrls.Accounts}/{accountId}/instruments", cancellationToken);
        }
    }
}
