using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Newtonsoft.Json;
using Polly;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    internal class OrderBookSnapshotsRepository
    {
        private const string _dateTimeFormatString = "yyyy-MM-ddTHH:mm:ss.fff";
        private const string _blobContainer = "orderbookssnapshots";
        private static readonly string _className = nameof(OrderBookSnapshotsRepository);

        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookSnapshotEntity> _tableStorage;
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

            // Retry to save 5 times
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(5)); 

            await retryPolicy.ExecuteAsync(async () =>
            {
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
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(_className,
                        $"Could not save orderbook snapshot {blobName} to DB. Retrying...", ex);
                    try
                    {
                        // Ensure that the table is not stored if the failure is because of the blob persistance
                        await _tableStorage.DeleteIfExistAsync(tableEntity.PartitionKey,
                            tableEntity.RowKey);
                    }
                    catch (Exception delException)
                    {
                        await _log.WriteErrorAsync(_className,
                            $"Could not delete row with Source {tableEntity.Exchange}, " +
                            $"Pair {tableEntity.AssetPair} and Snapshot date " +
                            $"{tableEntity.SnapshotDateTime} in table {_tableStorage.Name}.", delException);
                    }
                    throw;
                }
            });
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
