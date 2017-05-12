using System.Threading.Tasks;
using TradingBot.OandaApi.Entities.Accounts;

namespace TradingBot.OandaApi.ApiEndpoints
{
    public class AccountsApi : BaseApi
    {
        public AccountsApi(ApiClient apiClient) : base(apiClient)
        {
        }

        public Task<AccountsList> GetAccounts()
        {
            return ApiClient.MakeRequestAsync<AccountsList>(OandaUrls.Accounts);
        }

        public Task<AccountDetails> GetAccountDetails(string accountId)
        {
            return ApiClient.MakeRequestAsync<AccountDetails>($"{OandaUrls.Accounts}/{accountId}");
        }

        public Task<AccountInstrumentsResponse> GetAccountInstruments(string accountId)
        {
            return ApiClient.MakeRequestAsync<AccountInstrumentsResponse>($"{OandaUrls.Accounts}/{accountId}/instruments");
        }
    }
}
