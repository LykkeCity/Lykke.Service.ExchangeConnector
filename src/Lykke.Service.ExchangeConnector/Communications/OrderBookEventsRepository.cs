using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Helpers;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    internal class OrderBookEventsRepository
    {
        private const int _desiredQueueLength = 30;

        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookEventEntity> _tableStorage;
        private readonly Queue<OrderBookEventEntity> _orderBookEventEntities = new Queue<OrderBookEventEntity>();
        private static readonly string _className = nameof(OrderBookEventsRepository);
        private DateTime _lastSavedMinute;

        public OrderBookEventsRepository(INoSQLTableStorage<OrderBookEventEntity> tableStorage, 
            ILog log)
        {
            _tableStorage = tableStorage;
            _log = log;
        }

		public async Task SaveAsync(OrderBookEvent orderBookEvent)
		{
			if (_lastSavedMinute == default)
                _lastSavedMinute = DateTime.UtcNow.TruncSeconds();

            foreach (var orderItem in orderBookEvent.OrderItems)
                _orderBookEventEntities.Enqueue(new OrderBookEventEntity(
                    orderBookEvent.SnapshotId,
                    orderBookEvent.OrderEventTimestamp, orderItem.Id)
                    {
                        OrderId = orderItem.Id,
                        IsBuy = orderItem.IsBuy,
                        Symbol = orderItem.Symbol,
                        EventType = (int)orderBookEvent.EventType,
                        Price = orderItem.Price,
                        Size = orderItem.Size
                    });

            var currentTimeMinute = DateTime.UtcNow.TruncSeconds();
            var isDifferentMinute = currentTimeMinute != _lastSavedMinute;
            var isQueueMaxLength = _orderBookEventEntities.Count > _desiredQueueLength;

            // Save on certain amount of time or items count
            if (!isDifferentMinute && !isQueueMaxLength)  
		        return;

            var tableEntityBatches = _orderBookEventEntities
                .DequeueChunk(_desiredQueueLength)
                .GroupBy(ee => ee.PartitionKey);

            foreach (var currentBatch in tableEntityBatches)
            {
                try
                {
                    await _tableStorage.InsertOrReplaceBatchAsync(currentBatch);
                    //Mute for now
                    //await _log.WriteInfoAsync(_className, _className,
                    //    $"{currentBatch.Count()} order events for orderbook with snapshot {orderBookEvent.SnapshotId} " +
                    //    $"were published to Azure table {_tableStorage.Name}.");
                    _lastSavedMinute = currentTimeMinute;
                }
                catch (Exception ex)
                {
                    _orderBookEventEntities.AddRange(currentBatch);    // Queue the list back
                    await _log.WriteErrorAsync(_className,
                        $"Can't write to Azure Table {_tableStorage.Name} for snapshot {currentBatch.Key}, " + 
                        $"will try later. Now in queue: {_orderBookEventEntities.Count}", ex);
                }
            }
		}
    }
}
