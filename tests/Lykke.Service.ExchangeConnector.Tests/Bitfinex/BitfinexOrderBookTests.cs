using System.Threading.Tasks;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using Moq;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Repositories;
using TradingBot.Trading;
using Xunit;

namespace Lykke.Service.ExchangeConnector.Tests.Bitfinex
{
    public class BitfinexOrderBookTests
    {
        private readonly ILog _log;
        private readonly OrderBookSnapshotsRepository _snapshotsRepository;
        private readonly OrderBookEventsRepository _eventsRepository;
        private readonly BitfinexExchangeConfiguration _bitfinexConfiguration;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<OrderBook> _orderBookHandler;

        private const string _tableStorageEndpoint = "UseDevelopmentStorage=true";
        private const string _snapshotsTable = "orderBookSnapshots";
        private const string _orderBookEventsTable = "orderBookEvents";
        private const string _blobStorageEndpoint = "UseDevelopmentStorage=true";

        public BitfinexOrderBookTests()
        {
            _log = new LogToConsole();

            var settingsManager = BitfinexHelpers.GetBitfinexSettingsMenager();
            _bitfinexConfiguration = settingsManager.CurrentValue;

            var orderBookEventsStorage = AzureTableStorage<OrderBookEventEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _orderBookEventsTable, _log);
            var orderBookSnapshotStorage = AzureTableStorage<OrderBookSnapshotEntity>.Create(
                settingsManager.ConnectionString(i => _tableStorageEndpoint), _snapshotsTable, _log);
            var azureBlobStorage = AzureBlobStorage.Create(
                settingsManager.ConnectionString(i => _blobStorageEndpoint));

            _snapshotsRepository = new OrderBookSnapshotsRepository(orderBookSnapshotStorage, azureBlobStorage, _log);
            _eventsRepository = new OrderBookEventsRepository(orderBookEventsStorage, _log);

            _orderBookHandler = new Mock<IHandler<OrderBook>>().Object;
            _tickPriceHandler = new Mock<IHandler<TickPrice>>().Object;

        }

        [Fact]
        public async Task HarvestTicker()
        {
            var orderBookHarvester = new BitfinexOrderBooksHarvester(_bitfinexConfiguration, _snapshotsRepository, _eventsRepository, _orderBookHandler, _tickPriceHandler, _log);

            var tickerTcs = new TaskCompletionSource<TickPrice>();

            orderBookHarvester.Start();

            await Task.WhenAny(Task.Delay(10000), tickerTcs.Task);

            orderBookHarvester.Stop();

            Assert.True(tickerTcs.Task.IsCompletedSuccessfully);
        }
    }
}
