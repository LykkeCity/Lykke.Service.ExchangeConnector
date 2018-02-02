using Autofac;
using AzureStorage;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.ExchangeDataStore.AzureRepositories.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.SettingsReader;

namespace Lykke.Service.ExchangeDataStore.AzureRepositories
{
    public static class AzureRepoBinder
    {
        public static void BindAzureRepositories(this ContainerBuilder container, IReloadingManager<ExchangeDataStoreSettings> settings, ILog log)
        {
            var azureBlobStorage = AzureBlobStorage.Create(
                settings.ConnectionString(i => i.AzureStorage.EntitiesConnString));
            container.RegisterInstance(azureBlobStorage).As<IBlobStorage>().SingleInstance();

            var orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                settings.ConnectionString(i => i.AzureStorage.EntitiesConnString), settings.CurrentValue.AzureStorage.EntitiesTableName, log);
            container.RegisterInstance(orderBookSnapshotStorage).As<INoSQLTableStorage<OrderBookSnapshotEntity>>().SingleInstance();
            
            container.RegisterType<OrderBookSnapshotsRepository>().As<IOrderBookSnapshotsRepository>();
        }
    }
}
