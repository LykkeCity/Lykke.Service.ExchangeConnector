using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.ExternalExchangesApi.Helpers;
using Common.Log;
using TradingBot.Handlers;
using TradingBot.Trading;

namespace TradingBot.Communications
{
    internal class AzureTablePricesPublisher : IHandler<TickPrice>
    {
        private readonly ILog _log;
        private readonly INoSQLTableStorage<PriceTableEntity> tableStorage;
        private readonly string tableName;

        public AzureTablePricesPublisher(INoSQLTableStorage<PriceTableEntity> tableStorage, string tableName, ILog log)
        {
            _log = log;
            this.tableStorage = tableStorage;
            this.tableName = tableName;
        }

        private readonly Queue<TickPrice> pricesQueue = new Queue<TickPrice>();
        private readonly Queue<PriceTableEntity> tablePricesQueue = new Queue<PriceTableEntity>();
        private DateTime currentPriceMinute;
        private readonly ConcurrentDictionary<string, TickPrice> lastTickPrices = new ConcurrentDictionary<string, TickPrice>();

        public async Task Handle(TickPrice message)
        {
            if (lastTickPrices.TryGetValue(message.Instrument.Name, out var lastTickPrice))
            {
                if (lastTickPrice != null &&
                    lastTickPrice.Ask == message.Ask && lastTickPrice.Bid == message.Bid)
                {
                    return; // skip the same tickPrice, we not going to save it to the database
                }
                else
                {
                    lastTickPrices.TryUpdate(message.Instrument.Name, newValue: message, comparisonValue: lastTickPrice);
                }
            }
            else
            {
                lastTickPrices.TryAdd(message.Instrument.Name, message);
            }


            if (currentPriceMinute == default)
                currentPriceMinute = message.Time.TruncSeconds();

            var timeMunite = message.Time.TruncSeconds();

            bool nextMinute = timeMunite > currentPriceMinute;
            bool fieldOverflow = pricesQueue.Count >= MaxQueueCount;

            if (nextMinute || fieldOverflow)
            {
                var tablePrice = new PriceTableEntity(message.Instrument.Name,
                                                        currentPriceMinute,
                                                        pricesQueue.ToArray());

                pricesQueue.Clear();
                pricesQueue.Enqueue(message);
                currentPriceMinute = fieldOverflow ? message.Time.RoundToSecond() : timeMunite;

                try
                {
                    await tableStorage.InsertAsync(tablePrice);
                    await _log.WriteInfoAsync(nameof(AzureTablePricesPublisher), nameof(Handle), string.Empty, $"Prices for {message.Instrument} published to Azure table {tableName}");
                }
                catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                    when (ex.Message == "Conflict")
                {
                    await _log.WriteWarningAsync(nameof(AzureTablePricesPublisher), nameof(Handle), string.Empty, $"Conflict on writing. Skip chunk for {currentPriceMinute}");
                }
                catch (Exception)
                {
                    tablePricesQueue.Enqueue(tablePrice);
                    await _log.WriteWarningAsync(nameof(AzureTablePricesPublisher), nameof(Handle), string.Empty, $"Can't write to Azure Table Storage, will try later. Now in queue: {tablePricesQueue.Count}");
                }
            }
            else
            {
                pricesQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// One AzureTable field must be 64k or less. 
        /// Strings are stored in UTF16 encoding, so maximum number of characters is 32K.
        /// One serialized entry has size no more then 100 characters.
        /// </summary>
        private const int MaxQueueCount = 100;
    }
}
