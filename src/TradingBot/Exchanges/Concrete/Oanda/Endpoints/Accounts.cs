using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
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
            return ApiClient.MakeGetRequestAsync<AccountsList>(OandaUrls.Accounts);
        }

        public Task<AccountDetails> GetAccountDetails(string accountId)
        {
            return ApiClient.MakeGetRequestAsync<AccountDetails>($"{OandaUrls.Accounts}/{accountId}");
        }

        public Task<AccountInstrumentsResponse> GetAccountInstruments(string accountId)
        {
            return ApiClient.MakeGetRequestAsync<AccountInstrumentsResponse>($"{OandaUrls.Accounts}/{accountId}/instruments");
        }
    }
}
