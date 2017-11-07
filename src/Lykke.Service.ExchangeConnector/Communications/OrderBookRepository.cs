using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Communications
{
    public class OrderBookRepository
    {
        private const string _azureConflictExceptionReason = "Conflict";

        private readonly ILogger _logger = Logging.CreateLogger<OrderBookRepository>();
	    private readonly string _tableName;
        private readonly INoSQLTableStorage<OrderBookEntity> _tableStorage;
        private readonly Queue<OrderBook> _orderBooks = new Queue<OrderBook>();
        private readonly Queue<OrderBookEntity> _orderBookEntities = new Queue<OrderBookEntity>();
        private DateTime _currentPriceMinute;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.fff"
        };

        /// <summary>
        /// One AzureTable field must be 64k or less. 
        /// Strings are stored in UTF16 encoding, so maximum number of characters is 32K.
        /// One serialized entry has size no more then 100 characters.
        /// </summary>
        private const int MaxQueueCount = 100;

        public OrderBookRepository(INoSQLTableStorage<OrderBookEntity> tableStorage, string tableName)
        {
            this._tableStorage = tableStorage;
            this._tableName = tableName;
        }

		public async Task SaveAsync(OrderBook orderBook)
		{
			if (_currentPriceMinute == default)
                _currentPriceMinute = orderBook.Timestamp.TruncSeconds();

            var timeMunite = orderBook.Timestamp.TruncSeconds();

            bool nextMinute = timeMunite > _currentPriceMinute;
            bool fieldOverflow = _orderBooks.Count >= MaxQueueCount;

            if (nextMinute || fieldOverflow)
            {
                var asks = JsonSerializeVolumePriceList(orderBook.Asks);
                var bids = JsonSerializeVolumePriceList(orderBook.Bids);
                var entity = new OrderBookEntity(orderBook.Source, orderBook.AssetPairId,
                    orderBook.Timestamp, asks, bids);
                                                    
                _orderBooks.Clear();
                _orderBooks.Enqueue(orderBook);
                _currentPriceMinute = fieldOverflow ? orderBook.Timestamp.TruncMiliseconds() : timeMunite;
                
                try
                {
                    await _tableStorage.InsertAsync(entity);
                    _logger.LogDebug($"Orderbook for {orderBook.Source} and asset pair {orderBook.AssetPairId} published to Azure table {_tableName}");
                }
                catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                    when (ex.Message == _azureConflictExceptionReason)
                {
                    _logger.LogInformation($"Conflict on writing. Skip chunk for {_currentPriceMinute}");
                }
                catch (Exception ex)
                {
                    _orderBookEntities.Enqueue(entity);
                    _logger.LogError(0, ex,
                        $"Can't write to Azure Table {_tableName}, will try later. Now in queue: {_orderBookEntities.Count}");
                }
            }
            else
            {
                _orderBooks.Enqueue(orderBook);
            }
		}

        private string JsonSerializeVolumePriceList(IReadOnlyCollection<VolumePrice> volumePrices)
        {
            return JsonConvert.SerializeObject(volumePrices, _serializerSettings);
        }

        private IReadOnlyCollection<VolumePrice> JsonDeserializeVolumePriceList(string serializedVolumePrices)
        {
            return JsonConvert.DeserializeObject<IReadOnlyCollection<VolumePrice>>(
                serializedVolumePrices, _serializerSettings);
        }
    }
}
