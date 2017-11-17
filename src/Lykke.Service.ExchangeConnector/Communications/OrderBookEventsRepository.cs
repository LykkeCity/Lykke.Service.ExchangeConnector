using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Common.Log;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Helpers;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    internal class OrderBookEventsRepository
    {
        private const string _azureConflictExceptionReason = "Conflict";

        private readonly ILog _log;
        private readonly INoSQLTableStorage<OrderBookEventEntity> _tableStorage;
        private readonly Queue<OrderBookEvent> _orderBookEvents = new Queue<OrderBookEvent>();
        private readonly Queue<OrderBookEventEntity> _orderBookEventEntities = new Queue<OrderBookEventEntity>();
        private static readonly string _className = nameof(OrderBookSnapshotsRepository);
        private DateTime _currentMinute;

        /// <summary>
        /// One AzureTable field must be 64k or less. 
        /// Strings are stored in UTF16 encoding, so maximum number of characters is 32K.
        /// One serialized entry has size no more then 100 characters.
        /// </summary>
        private const int MaxQueueCount = 1;

        public OrderBookEventsRepository(INoSQLTableStorage<OrderBookEventEntity> tableStorage, 
            ILog log)
        {
            _tableStorage = tableStorage;
            _log = log;
        }

		public async Task SaveAsync(OrderBookEvent orderBookEvent)
		{
			if (_currentMinute == default)
                _currentMinute = orderBookEvent.InternalTimestamp.TruncSeconds();

            var timeMunite = orderBookEvent.InternalTimestamp.TruncSeconds();

            bool nextMinute = timeMunite > _currentMinute;
            bool fieldOverflow = _orderBookEvents.Count >= MaxQueueCount;

		    // Save on certain amount of time or items count
            if (!nextMinute && !fieldOverflow)  
		    {
		        _orderBookEvents.Enqueue(orderBookEvent);
		        return;
		    }

		    var tableEntities = orderBookEvent.OrderItems.Select(oi =>
		        new OrderBookEventEntity(orderBookEvent.SnapshotId,
		            orderBookEvent.OrderEventTimestamp, oi.Id)
		        {
		            OrderId = oi.Id,
		            IsBuy = oi.IsBuy,
		            Symbol = oi.Symbol,
		            EventType = (int) orderBookEvent.EventType,
		            Price = oi.Price,
		            Size = oi.Size
		        })
                .ToList();

            _orderBookEvents.Clear();
            _orderBookEvents.Enqueue(orderBookEvent);
            _currentMinute = fieldOverflow ? orderBookEvent.InternalTimestamp.TruncMiliseconds() : timeMunite;

            try
            {
                await _tableStorage.InsertOrReplaceBatchAsync(tableEntities);
                await _log.WriteInfoAsync(_className, _className,
                    $"{tableEntities.Count} order events for orderbook with snapshot {orderBookEvent.SnapshotId} were " + 
                    $"published to Azure table {_tableStorage.Name}.");
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                when (ex.Message == _azureConflictExceptionReason)
            {
                await _log.WriteErrorAsync(_className, 
                    $"Conflict on writing. Skip chunk for {_currentMinute}", ex);
            }
            catch (Exception ex)
            {
                foreach (var entity in tableEntities)
                    _orderBookEventEntities.Enqueue(entity);
                await _log.WriteErrorAsync(_className,
                    $"Can't write to Azure Table {_tableStorage.Name}, will try later. Now in queue: " + 
                    $"{_orderBookEventEntities.Count}", ex);
            }
		}
    }
}
