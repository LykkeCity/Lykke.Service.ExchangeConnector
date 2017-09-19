using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Oanda.Endpoints;
using System;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Oanda
{
    public class OandaExchange : Exchange
    {
        public new static readonly string Name = "oanda";
        
        private Accounts accounts;
        private Prices prices;
        private Instruments instruments;

        public OandaExchange(OandaConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, ILog log) : 
            base(Name, config, translatedSignalsRepository, log)
        {
            var client = new ApiClient(OandaHttpClient.CreateHttpClient(OandaAuth.Token), log);

            accounts = new Accounts(client);
            prices = new Prices(client);
            instruments = new Instruments(client);
        }

        protected override void StartImpl()
        {
            throw new NotImplementedException();
        }

        protected override void StopImpl()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected async Task<bool> EstablishConnectionImpl(CancellationToken cancellationToken)
        {
            var accountsList = await accounts.GetAccounts(cancellationToken);
            

            var accountId = accountsList.Accounts.First().Id;

            return !string.IsNullOrEmpty(accountId);
        }
    }
}
