using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Blob;
using Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    public class OrderBookRepository
    {
        private const string _azureConflictExceptionReason = "Conflict";
        private const string _dateTimeFormatString = "yyyy-MM-ddTHH:mm:ss.fff";
        private const string _dateTimeBlobNameFormatString = "yyyy-MM-ddTHH-mm-ss.fff";

        private readonly string _blobContainer;
        private readonly ILogger _logger = Logging.CreateLogger<OrderBookRepository>();
	    private readonly string _tableName;
        private readonly INoSQLTableStorage<OrderBookEntity> _tableStorage;
        private readonly Queue<OrderBookSnapshot> _orderBooks = new Queue<OrderBookSnapshot>();
        private readonly Queue<OrderBookEntity> _orderBookEntities = new Queue<OrderBookEntity>();
        private readonly AzureBlobStorage _blobStorage;
        private DateTime _currentPriceMinute;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DateFormatString = _dateTimeFormatString
        };

        /// <summary>
        /// One AzureTable field must be 64k or less. 
        /// Strings are stored in UTF16 encoding, so maximum number of characters is 32K.
        /// One serialized entry has size no more then 100 characters.
        /// </summary>
        private const int MaxQueueCount = 100;

        public OrderBookRepository(INoSQLTableStorage<OrderBookEntity> tableStorage, string tableName,
           AzureBlobStorage blobStorage, string blobContainer)
        {
            _tableStorage = tableStorage;
            _tableName = tableName;
            _blobStorage = blobStorage;
            _blobContainer = blobContainer;
        }

		public async Task SaveAsync(OrderBookSnapshot orderBook)
		{
			if (_currentPriceMinute == default)
                _currentPriceMinute = orderBook.Timestamp.TruncSeconds();

            var timeMunite = orderBook.Timestamp.TruncSeconds();

            bool nextMinute = timeMunite > _currentPriceMinute;
            bool fieldOverflow = _orderBooks.Count >= MaxQueueCount;

            if (nextMinute || fieldOverflow)
            {
                var orders = orderBook.Asks.Values.Union(orderBook.Bids.Values);
                var tableEntity = new OrderBookEntity(orderBook.Source, orderBook.AssetPair, orderBook.Timestamp);
                var serializedOrders = JsonSerializeVolumePriceList(orders);
                
                _orderBooks.Clear();
                _orderBooks.Enqueue(orderBook);
                _currentPriceMinute = fieldOverflow ? orderBook.Timestamp.TruncMiliseconds() : timeMunite;

                var blobName = GetBlobName(orderBook);
                try
                {
                    // TODO: Create the table
                    await _tableStorage.InsertAsync(tableEntity);
                    await _blobStorage.SaveBlobAsync(_blobContainer, blobName, 
                        Encoding.UTF8.GetBytes(serializedOrders));
                    _logger.LogDebug($"Orderbook for {orderBook.Source} and asset pair {orderBook.AssetPair} " + 
                        $"published to Azure table {_tableName}. Orders published to blob container {_blobContainer} and " +
                        $"blob {blobName}");
                }
                catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                    when (ex.Message == _azureConflictExceptionReason)
                {
                    _logger.LogError(ex, $"Conflict on writing. Skip chunk for {_currentPriceMinute}");
                    try
                    {
                        await _tableStorage.DeleteIfExistAsync(tableEntity.PartitionKey,
                            tableEntity.RowKey);
                    }
                    catch (Exception delException)
                    {
                        _logger.LogError(delException, 
                            $"Could not delete row with Source {tableEntity.Source}, " +
                            $"Pair {tableEntity.AssetPair} and Dnapshot date " + 
                            $"{tableEntity.SnapshotDateTime} in table {_tableName}.");
                    }
                }
                catch (Exception ex)
                {
                    _orderBookEntities.Enqueue(tableEntity);
                    _logger.LogError(0, ex,
                        $"Can't write to Azure Table {_tableName}, will try later. Now in queue: {_orderBookEntities.Count}");
                }
            }
            else
            {
                _orderBooks.Enqueue(orderBook);
            }
		}

        private string GetBlobName(OrderBookSnapshot orderBook)
        {
            return $"{orderBook.Source}_{RemoveSpecialCharacters(orderBook.AssetPair)}_" + 
                $"{orderBook.Timestamp.Date.ToString(_dateTimeBlobNameFormatString)}";
        }

        public static string RemoveSpecialCharacters(string str)
        {
            return new string(str.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray());
        }

        private string JsonSerializeVolumePriceList(IEnumerable<OrderBookItem> orderItems)
        {
            return JsonConvert.SerializeObject(orderItems, _serializerSettings);
        }

        private ICollection<OrderBookItem> JsonDeserializeVolumePriceList(string serializedVolumePrices)
        {
            return JsonConvert.DeserializeObject<ICollection<OrderBookItem>>(
                serializedVolumePrices, _serializerSettings);
        }
    }
}
