using Lykke.Service.ExchangeDataStore.Core.Services;
using Lykke.Service.ExchangeDataStore.Services.DataHarvesters;
using Lykke.Service.ExchangeDataStore.Services.DataPersisters;
using System.Threading.Tasks;

// ReSharper disable ClassNeverInstantiated.Global

namespace Lykke.Service.ExchangeDataStore.Services
{
    // NOTE: Sometimes, shutdown process should be expressed explicitly. 
    // If this is your case, use this class to manage shutdown.
    // For example, sometimes some state should be saved only after all incoming message processing and 
    // all periodical handler was stopped, and so on.

    public class ShutdownManager : IShutdownManager
    {
        private readonly OrderbookDataPersister _dataPersister;
        private readonly OrderbookDataHarvester _dataHarvester;

        public ShutdownManager(OrderbookDataPersister dataPersister, OrderbookDataHarvester dataHarvester)
        {
            _dataPersister = dataPersister;
            _dataHarvester = dataHarvester;
        }

        public async Task StopAsync()
        {
            _dataHarvester.Stop();
            _dataPersister.Stop();

            await Task.CompletedTask;
        }
    }
}
