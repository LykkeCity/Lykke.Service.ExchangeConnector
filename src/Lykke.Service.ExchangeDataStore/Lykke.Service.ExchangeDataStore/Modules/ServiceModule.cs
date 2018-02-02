using Autofac;
using Common.Log;
using Lykke.Service.ExchangeDataStore.AzureRepositories;
using Lykke.Service.ExchangeDataStore.Core.Services;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Services;
using Lykke.Service.ExchangeDataStore.Services.DataHarvesters;
using Lykke.Service.ExchangeDataStore.Services.DataPersisters;
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
            RegisterRepos(builder);
            RegisterLocalTypes(builder);
            
        }

        private void RegisterLocalTypes(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.AzureStorage);
            builder.RegisterType<OrderbookDataHarvester>().As<IStartable>().WithParameter("orderBookQueueConfig", _settings.CurrentValue.RabbitMq.OrderBooks).SingleInstance();
            builder.RegisterType<OrderbookDataPersister>().As<IStartable>().SingleInstance();
            builder.RegisterType<HealthService>().As<IHealthService>().SingleInstance();
            builder.RegisterType<StartupManager>().As<IStartupManager>();
            builder.RegisterType<ShutdownManager>().As<IShutdownManager>();
        }

        private void RegisterRepos(ContainerBuilder builder)
        {
            builder.BindAzureRepositories(_settings, _log);
        }
    }
}
