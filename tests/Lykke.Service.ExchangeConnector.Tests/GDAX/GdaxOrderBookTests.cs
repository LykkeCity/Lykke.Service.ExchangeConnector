using System.Threading.Tasks;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Repositories;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.GDAX
{
    public class GdaxOrderBookTests
    {
        private readonly ILog _log;
        private readonly OrderBookSnapshotsRepository _snapshotsRepository;
        private readonly OrderBookEventsRepository _eventsRepository;
        private readonly GdaxExchangeConfiguration _gdaxConfiguration;

        private const string _tableStorageEndpoint = "UseDevelopmentStorage=true";
        private const string _snapshotsTable = "orderBookSnapshots";
        private const string _orderBookEventsTable = "orderBookEvents";
        private const string _blobStorageEndpoint = "UseDevelopmentStorage=true";

        public GdaxOrderBookTests()
        {
            _log = new LogToConsole();

            var settingsManager = GdaxHelpers.GetGdaxSettingsMenager();
             _gdaxConfiguration = settingsManager.CurrentValue;

            var orderBookEventsStorage = AzureTableStorage<OrderBookEventEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _orderBookEventsTable, _log);
            var orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _snapshotsTable, _log);
            var azureBlobStorage = AzureBlobStorage.Create(
                settingsManager.ConnectionString(i => _blobStorageEndpoint));

            _snapshotsRepository = new OrderBookSnapshotsRepository(orderBookSnapshotStorage, azureBlobStorage, _log);
            _eventsRepository = new OrderBookEventsRepository(orderBookEventsStorage, _log);
        }

        [Fact]
        public async Task HarvestAndPersist()
        {
            var orderBookHarvester = new GdaxOrderBooksHarvester("Gdax", _gdaxConfiguration, _log,
                _snapshotsRepository, _eventsRepository);
            orderBookHarvester.Start();

            await Task.Delay(1000000);

            orderBookHarvester.Stop();
        }
    }
}
