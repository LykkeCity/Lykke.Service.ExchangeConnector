using Lykke.Service.ExchangeDataStore.Core.Services;
using Lykke.Service.ExchangeDataStore.Services.DataHarvesters;
using Lykke.Service.ExchangeDataStore.Services.DataPersisters;
using System.Threading.Tasks;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Local

namespace Lykke.Service.ExchangeDataStore.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        private readonly OrderbookDataPersister _dataPersister;
        private readonly OrderbookDataHarvester _dataHarvester;



        public StartupManager(OrderbookDataPersister dataPersister, OrderbookDataHarvester dataHarvester)
        {
            _dataPersister = dataPersister;
            _dataHarvester = dataHarvester;
        }

        public async Task StartAsync()
        {
            _dataPersister.Start();
            _dataHarvester.Start();

             await Task.CompletedTask;
        }
    }
}
