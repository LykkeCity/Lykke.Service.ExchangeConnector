using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    internal class OrderBookSnapshotsRepository
    {
        private const string _azureConflictExceptionReason = "Conflict";
        private const string _dateTimeFormatString = "yyyy-MM-ddTHH:mm:ss.fff";
        private const string _blobContainer = "orderbookssnapshots";
        private static readonly string _className = nameof(OrderBookSnapshotsRepository);

        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookSnapshotEntity> _tableStorage;
        private readonly Queue<OrderBookSnapshotEntity> _orderBookEntities = new Queue<OrderBookSnapshotEntity>();
        private readonly IBlobStorage _blobStorage;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DateFormatString = _dateTimeFormatString
        };

        public OrderBookSnapshotsRepository(INoSQLTableStorage<OrderBookSnapshotEntity> tableStorage, 
           IBlobStorage blobStorage, ILog log)
        { 
            _tableStorage = tableStorage;
            _blobStorage = blobStorage;
            _log = log;
        }

		public async Task SaveAsync(OrderBookSnapshot orderBook)
		{
            var orders = orderBook.Asks.Values.Union(orderBook.Bids.Values);
            var tableEntity = new OrderBookSnapshotEntity(orderBook.Source, orderBook.AssetPair, orderBook.OrderBookTimestamp);
            var serializedOrders = JsonSerializeVolumePriceList(orders);

            var blobName = tableEntity.UniqueId;
		    try
		    {
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
		        _orderBookEntities.Enqueue(tableEntity);
                await _log.WriteErrorAsync(_className,
		            $"Conflict on writing. Skip chunk with timestamp {orderBook.InternalTimestamp}", ex);
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
