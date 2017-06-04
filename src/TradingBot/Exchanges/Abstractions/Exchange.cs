using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Infrastructure;
using System;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Abstractions
{
    public abstract class Exchange
    {
        protected ILogger Logger = Logging.CreateLogger<Exchange>();

        private string name;

        public string Name => name;

        public Exchange(string name)
        {
            this.name = name;
        }

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


        public abstract Task OpenPricesStream(Instrument[] instruments, Action<TickPrice[]> callback);

        public abstract void ClosePricesStream();
    }
}
