using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda.Endpoints;
using System;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Oanda
{
    public class OandaExchange : Exchange
    {
        public new static readonly string Name = "oanda";
        
        private Accounts accounts;
        private Prices prices;
        private Instruments instruments;

        public OandaExchange(OandaConfiguration config) : base(Name, config)
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

        protected override Task<bool> AddOrder(Instrument instrument, TradingSignal signal)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> CancelOrder(Instrument instrument, TradingSignal signal)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task OpenPricesStream()
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
