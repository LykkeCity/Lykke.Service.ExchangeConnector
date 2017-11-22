using System.Threading.Tasks;
using AzureStorage;
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
        private readonly INoSQLTableStorage<OrderBookEventEntity> _orderBookEventsStorage;
        private readonly INoSQLTableStorage<OrderBookSnapshotEntity> _orderBookSnapshotStorage;
        private readonly IBlobStorage _azureBlobStorage;
        private readonly OrderBookSnapshotsRepository _snapshotsRepository;
        private readonly OrderBookEventsRepository _eventsRepository;
        private readonly GdaxExchangeConfiguration _gdaxConfiguration;

        private const string _tableStorageEndpoint = "UseDevelopmentStorage=true";
        private const string _snapshotsTable = "orderBookSnapshots";
        private const string _orderBookEventsTable = "orderBookEvents";
        private const string _blobStorageEndpoint = "UseDevelopmentStorage=true";

        private const string _btcUsd = "BTCUSD";
        private const string _orderDoneTypeName = "done";
        private const string _orderCanceledReason = "canceled";

        public GdaxOrderBookTests()
        {
            _log = new LogToConsole();

            var settingsManager = GdaxHelpers.GetGdaxSettingsMenager();
             _gdaxConfiguration = settingsManager.CurrentValue;

            _orderBookEventsStorage = AzureTableStorage<OrderBookEventEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _orderBookEventsTable, _log);
            _orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _snapshotsTable, _log);
            _azureBlobStorage = AzureBlobStorage.Create(
                settingsManager.ConnectionString(i => _blobStorageEndpoint));

            _snapshotsRepository = new OrderBookSnapshotsRepository(_orderBookSnapshotStorage, _azureBlobStorage, _log);
            _eventsRepository = new OrderBookEventsRepository(_orderBookEventsStorage, _log);
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
