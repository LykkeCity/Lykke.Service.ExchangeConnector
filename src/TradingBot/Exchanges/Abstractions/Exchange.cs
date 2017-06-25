using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Trading;
using TradingBot.Common.Infrastructure;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Abstractions
{
    public abstract class Exchange
    {
        protected ILogger Logger = Logging.CreateLogger<Exchange>();

        private readonly string name;

        public string Name => name;

        protected Exchange(string name, IExchangeConfiguration config)
        {
            this.name = name;

            if (config.Instruments == null || config.Instruments.Length == 0)
            {
                throw new ArgumentException($"There is no instruments in the settings for {name} exchange");
            }
            
            this.Instruments = config.Instruments.Select(x => new Instrument(x)).ToList();
        }

        public IReadOnlyList<Instrument> Instruments { get; protected set; }

        public Task<bool> TestConnection()
        {
            return TestConnection(CancellationToken.None);
        }

        public async Task<bool> TestConnection(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Trying to test connection...");

            try
            {
				bool result = await TestConnectionImpl(cancellationToken);

				if (result)
				{
					Logger.LogInformation("Connection tested successfully.");
				}
				else
				{
					Logger.LogError("Connection test failed.");
				}

				return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(new EventId(), ex, "Connection test failed with error");
                return false;
            }

        }

        protected abstract Task<bool> TestConnectionImpl(CancellationToken cancellationToken);

        //public abstract Task<AccountInfo> GetAccountInfo(CancellationToken cancellationToken);


        public abstract Task OpenPricesStream(Action<InstrumentTickPrices> callback);

        public abstract void ClosePricesStream();
    }
}
