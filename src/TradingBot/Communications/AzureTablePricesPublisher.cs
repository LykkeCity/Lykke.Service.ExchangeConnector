using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot.Communications
{
    public class AzureTablePricesPublisher
    {
		private readonly ILogger logger = Logging.CreateLogger<AzureTablePricesPublisher>();

        private readonly Instrument instrument;

        private readonly INoSQLTableStorage<PriceTableEntity> tableStorage;

	    private readonly string partitionKey;
	    private readonly string tableName;

        public AzureTablePricesPublisher(Instrument instrument, string tableName, string connectionString)
        {
            this.instrument = instrument;
	        this.tableName = tableName;
	        this.partitionKey = instrument.Name.Replace("/", "").Replace("-", "");

			tableStorage = new AzureTableStorage<PriceTableEntity>(connectionString,
													   tableName,
													   new LogToConsole());
        }


		private readonly Queue<TickPrice> pricesQueue = new Queue<TickPrice>();
		private readonly Queue<PriceTableEntity> tablePricesQueue = new Queue<PriceTableEntity>();
		private DateTime currentPriceMinute;

		public async Task Publish(InstrumentTickPrices prices)
		{
			if (currentPriceMinute == default(DateTime))
                currentPriceMinute = prices.TickPrices[0].Time.TruncSeconds();

            foreach (var price in prices.TickPrices)
			{
				var timeMunite = price.Time.TruncSeconds();

				bool nextMinute = timeMunite > currentPriceMinute;
				bool fieldOverflow = pricesQueue.Count >= MaxQueueCount;

				if (nextMinute || fieldOverflow)
				{
					var tablePrice = new PriceTableEntity(partitionKey,
															currentPriceMinute,
															pricesQueue.ToArray());
														
					pricesQueue.Clear();
					pricesQueue.Enqueue(price);
					currentPriceMinute = fieldOverflow ? price.Time.TruncMiliseconds() : timeMunite;
					
					try
					{
						await tableStorage.InsertAsync(tablePrice);
						logger.LogDebug($"Prices for {partitionKey} published to Azure table {tableName}");
					}
					catch (Microsoft.WindowsAzure.Storage.StorageException ex)
						when (ex.Message == "Conflict")
					{
						logger.LogInformation($"Conflict on writing. Skip chunk for {currentPriceMinute}");
					}
					catch (Exception ex)
					{
						tablePricesQueue.Enqueue(tablePrice);
						logger.LogError(new EventId(), ex,
							$"Can't write to Azure Table Storage, will try later. Now in queue: {tablePricesQueue.Count}");
					}
				}
				else
				{
					pricesQueue.Enqueue(price);
				}
			}
		}

	    /// <summary>
	    /// One AzureTable field must be 64k or less. 
	    /// Strings are stored in UTF16 encoding, so maximum number of characters is 32K.
	    /// One serialized entry has size of 80 characters on average.
	    /// 32k / 80 = 400
	    /// </summary>
	    private const int MaxQueueCount = 400;
    }
}
