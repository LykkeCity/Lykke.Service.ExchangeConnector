using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
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
    internal class OandaExchange : Exchange
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

        public override Task<OrderStatusUpdate> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public override Task<OrderStatusUpdate> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
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
