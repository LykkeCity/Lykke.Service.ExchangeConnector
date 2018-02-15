using Autofac;
using AzureStorage;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.ExchangeDataStore.AzureRepositories.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Services;
using Lykke.Service.ExchangeDataStore.Core.Services.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Services;
using Lykke.Service.ExchangeDataStore.Services.DataHarvesters;
using Lykke.Service.ExchangeDataStore.Services.DataPersisters;
using Lykke.Service.ExchangeDataStore.Services.Domain;
using Lykke.SettingsReader;

namespace Lykke.Service.ExchangeDataStore.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<ExchangeDataStoreSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<ExchangeDataStoreSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            BindAzureRepositories(builder);
            RegisterLocalTypes(builder);
            RegisterLocalServices(builder);
        }

        private void RegisterLocalTypes(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.AzureStorage);
            builder.RegisterType<OrderbookDataHarvester>().WithParameter("orderBookQueueConfig", _settings.CurrentValue.RabbitMq.OrderBooks).SingleInstance();
            builder.RegisterType<OrderbookDataPersister>().SingleInstance();
            builder.RegisterType<HealthService>().As<IHealthService>().SingleInstance();
            builder.RegisterType<StartupManager>().As<IStartupManager>();
            builder.RegisterType<ShutdownManager>().As<IShutdownManager>();
        }

        private void BindAzureRepositories(ContainerBuilder container)
        {
            var azureBlobStorage = AzureBlobStorage.Create(
                _settings.ConnectionString(i => i.AzureStorage.EntitiesConnString));
            container.RegisterInstance(azureBlobStorage).As<IBlobStorage>().SingleInstance();

            var orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                _settings.ConnectionString(i => i.AzureStorage.EntitiesConnString), _settings.CurrentValue.AzureStorage.EntitiesTableName, _log);
            container.RegisterInstance(orderBookSnapshotStorage).As<INoSQLTableStorage<OrderBookSnapshotEntity>>().SingleInstance();

            container.RegisterType<OrderBookRepository>().As<IOrderBookRepository>();
            container.RegisterType<OrderBookSnapshotsRepository>().As<IOrderBookSnapshotsRepository>();
        }

        private void RegisterLocalServices(ContainerBuilder builder)
        {
            builder.RegisterType<OrderBookService>().As<IOrderBookService>();
        }
    }
}
