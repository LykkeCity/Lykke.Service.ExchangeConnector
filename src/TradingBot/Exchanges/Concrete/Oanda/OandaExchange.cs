using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda.Endpoints;
using System;
using TradingBot.Common.Trading;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.Oanda
{
    public class OandaExchange : Exchange
    {
        private Accounts accounts;
        private Prices prices;
        private Instruments instruments;

        public OandaExchange(OandaConfiguration config) : base("OANDA", config)
        {
            var client = new ApiClient(OandaHttpClient.CreateHttpClient(OandaAuth.Token));

            accounts = new Accounts(client);
            prices = new Prices(client);
            instruments = new Instruments(client);
        }

        public override void ClosePricesStream()
        {
            throw new NotImplementedException();
        }

        public override Task OpenPricesStream(Action<InstrumentTickPrices> callback)
        {
            throw new NotImplementedException();
        }

        protected override async Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            var accountsList = await accounts.GetAccounts(cancellationToken);
            Logger.LogDebug($"Received {accountsList.Accounts.Count} accounts");

            var accountId = accountsList.Accounts.First().Id;

            return !string.IsNullOrEmpty(accountId);
        }
    }
}
