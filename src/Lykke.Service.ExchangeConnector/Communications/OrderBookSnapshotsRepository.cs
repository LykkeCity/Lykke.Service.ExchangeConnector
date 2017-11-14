using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Blob;
using Common;
using Common.Log;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    internal class OrderBookSnapshotsRepository
    {
        private const string _azureConflictExceptionReason = "Conflict";
        private const string _dateTimeFormatString = "yyyy-MM-ddTHH:mm:ss.fff";
        private const string _dateTimeBlobNameFormatString = "yyyy-MM-ddTHH-mm-ss.fff";
        private static readonly string _className = nameof(OrderBookSnapshotsRepository);

        private const string _blobContainer = "orderbookssnapshots";
        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookSnapshotEntity> _tableStorage;
        private readonly Queue<OrderBookSnapshot> _orderBooks = new Queue<OrderBookSnapshot>();
        private readonly Queue<OrderBookSnapshotEntity> _orderBookEntities = new Queue<OrderBookSnapshotEntity>();
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

        public OrderBookSnapshotsRepository(INoSQLTableStorage<OrderBookSnapshotEntity> tableStorage, 
           AzureBlobStorage blobStorage, ILog log)
        { 
            _tableStorage = tableStorage;
            _blobStorage = blobStorage;
            _log = log;
        }

		public async Task SaveAsync(OrderBookSnapshot orderBook)
		{
			if (_currentPriceMinute == default)
                _currentPriceMinute = orderBook.InternalTimestamp.TruncSeconds();

            var timeMunite = orderBook.InternalTimestamp.TruncSeconds();

            bool nextMinute = timeMunite > _currentPriceMinute;
            bool fieldOverflow = _orderBooks.Count >= MaxQueueCount;

            if (nextMinute || fieldOverflow)
            {
                var orders = orderBook.Asks.Values.Union(orderBook.Bids.Values);
                var tableEntity = new OrderBookSnapshotEntity(orderBook.Source, orderBook.AssetPair, orderBook.OrderBookTimestamp);
                var serializedOrders = JsonSerializeVolumePriceList(orders);
                
                _orderBooks.Clear();
                _orderBooks.Enqueue(orderBook);
                _currentPriceMinute = fieldOverflow 
                    ? orderBook.InternalTimestamp.TruncMiliseconds() 
                    : timeMunite;

                var blobName = GetBlobName(orderBook);
                try
                {
                    // TODO: Create the table
                    await _tableStorage.InsertAsync(tableEntity);
                    await _blobStorage.SaveBlobAsync(_blobContainer, blobName, 
                        Encoding.UTF8.GetBytes(serializedOrders));
                    await _log.WriteInfoAsync(_className, _className,
                        $"Orderbook for {orderBook.Source} and asset pair {orderBook.AssetPair} " + 
                        $"published to Azure table {_tableStorage.Name}. Orders published to blob container {_blobContainer} and " +
                        $"blob {blobName}");

                    orderBook.GeneratedId = tableEntity.UniqueId;
                }
                catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                    when (ex.Message == _azureConflictExceptionReason)
                {
                    await _log.WriteErrorAsync(_className,
                        $"Conflict on writing. Skip chunk for {_currentPriceMinute}", ex);
                    try
                    {
                        await _tableStorage.DeleteIfExistAsync(tableEntity.PartitionKey,
                            tableEntity.RowKey);
                    }
                    catch (Exception delException)
                    {
                        await _log.WriteErrorAsync(_className,
                            $"Could not delete row with Source {tableEntity.Exchange}, " +
                            $"Pair {tableEntity.AssetPair} and Dnapshot date " + 
                            $"{tableEntity.SnapshotDateTime} in table {_tableStorage.Name}.", delException);
                    }
                }
                catch (Exception ex)
                {
                    _orderBookEntities.Enqueue(tableEntity);
                    await _log.WriteErrorAsync(_className,
                        $"Can't write to Azure Table {_tableStorage.Name}, will try later. " + 
                        $"Now in queue: {_orderBookEntities.Count}", ex);
                }
            }
            else
            {
                _orderBooks.Enqueue(orderBook);
            }
		}

        private string GetBlobName(OrderBookSnapshot orderBook)
        {
            return $"{orderBook.Source}_{orderBook.AssetPair}_" + 
                $"{orderBook.OrderBookTimestamp.Date.ToString(_dateTimeBlobNameFormatString)}"
                .RemoveSpecialCharacters();
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
