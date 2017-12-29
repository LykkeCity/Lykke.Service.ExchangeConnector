using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.ExternalExchangesApi.Helpers;
using Microsoft.Extensions.Logging;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot.Communications
{
    public class AzureTablePricesPublisher : IHandler<TickPrice>
    {
		private readonly ILogger logger = Logging.CreateLogger<AzureTablePricesPublisher>();

        private readonly INoSQLTableStorage<PriceTableEntity> tableStorage;

	    private readonly string tableName;

        public AzureTablePricesPublisher(INoSQLTableStorage<PriceTableEntity> tableStorage, string tableName)
        {
            this.tableStorage = tableStorage;
            this.tableName = tableName;
        }

		private readonly Queue<TickPrice> pricesQueue = new Queue<TickPrice>();
		private readonly Queue<PriceTableEntity> tablePricesQueue = new Queue<PriceTableEntity>();
		private DateTime currentPriceMinute;
        private readonly ConcurrentDictionary<string, TickPrice> lastTickPrices = new ConcurrentDictionary<string, TickPrice>();

		public async Task Handle(TickPrice tickPrice)
		{
		    if (lastTickPrices.TryGetValue(tickPrice.Instrument.Name, out var lastTickPrice))
		    {
		        if (lastTickPrice != null &&
		            lastTickPrice.Ask == tickPrice.Ask && lastTickPrice.Bid == tickPrice.Bid)
		        {
		            return; // skip the same tickPrice, we not going to save it to the database
		        }
		        else
		        {
		            lastTickPrices.TryUpdate(tickPrice.Instrument.Name, newValue: tickPrice, comparisonValue: lastTickPrice);
		        }
		    }
		    else
		    {
		        lastTickPrices.TryAdd(tickPrice.Instrument.Name, tickPrice);
		    }
		    
		    
			if (currentPriceMinute == default)
                currentPriceMinute = tickPrice.Time.TruncSeconds();

            var timeMunite = tickPrice.Time.TruncSeconds();

            bool nextMinute = timeMunite > currentPriceMinute;
            bool fieldOverflow = pricesQueue.Count >= MaxQueueCount;

            if (nextMinute || fieldOverflow)
            {
                var tablePrice = new PriceTableEntity(tickPrice.Instrument.Name,
                                                        currentPriceMinute,
                                                        pricesQueue.ToArray());
                                                    
                pricesQueue.Clear();
                pricesQueue.Enqueue(tickPrice);
                currentPriceMinute = fieldOverflow ? tickPrice.Time.TruncMiliseconds() : timeMunite;
                
                try
                {
                    await tableStorage.InsertAsync(tablePrice);
                    logger.LogDebug($"Prices for {tickPrice.Instrument} published to Azure table {tableName}");
                }
                catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                    when (ex.Message == "Conflict")
                {
                    logger.LogInformation($"Conflict on writing. Skip chunk for {currentPriceMinute}");
                }
                catch (Exception ex)
                {
                    tablePricesQueue.Enqueue(tablePrice);
                    logger.LogError(0, ex,
                        $"Can't write to Azure Table Storage, will try later. Now in queue: {tablePricesQueue.Count}");
                }
            }
            else
            {
                pricesQueue.Enqueue(tickPrice);
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
