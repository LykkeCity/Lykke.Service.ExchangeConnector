using System;
using System.IO;
using System.Threading.Tasks;
using TradingBot.Exchanges.OandaApi;
using TradingBot.Exchanges.OandaApi.ApiEndpoints;
using Xunit;

namespace TradingBot.Tests
{
    public class AccountsApiTests
    {
        private string GetToken => OandaAuth.Token;
        private string accountId => "{PLACE-YOUR-ACCOUNTID-HERE}";

        private AccountsApi CreateAccountsApi()
        {
            return new AccountsApi(new ApiClient(GetToken));
        }

        [Fact]
        public async Task GetAccountsDetails()
        {
            var accounts = CreateAccountsApi();

            var result = await accounts.GetAccounts();

            Assert.Single(result.Accounts);
        }

        [Fact]
        public async Task GetAccountDetails()
        {
            var api = CreateAccountsApi();

            var result = await api.GetAccountDetails(accountId);
        }

        [Fact]
        public async Task GetAcccountInstruments()
        {
            var api = CreateAccountsApi();

            var result = await api.GetAccountInstruments(accountId);
        }
    }
}
