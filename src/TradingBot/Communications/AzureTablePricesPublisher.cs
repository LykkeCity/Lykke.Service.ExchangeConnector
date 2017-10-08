using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Microsoft.Extensions.Logging;
using TradingBot.Handlers;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot.Communications
{
    public class AzureTablePricesPublisher : Handler<InstrumentTickPrices>
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

		public override async Task Handle(InstrumentTickPrices prices)
		{
			if (currentPriceMinute == default)
                currentPriceMinute = prices.TickPrices[0].Time.TruncSeconds();

            foreach (var price in prices.TickPrices)
			{
				var timeMunite = price.Time.TruncSeconds();

				bool nextMinute = timeMunite > currentPriceMinute;
				bool fieldOverflow = pricesQueue.Count >= MaxQueueCount;

				if (nextMinute || fieldOverflow)
				{
					var tablePrice = new PriceTableEntity(prices.Instrument.Name,
															currentPriceMinute,
															pricesQueue.ToArray());
														
					pricesQueue.Clear();
					pricesQueue.Enqueue(price);
					currentPriceMinute = fieldOverflow ? price.Time.TruncMiliseconds() : timeMunite;
					
					try
					{
						await tableStorage.InsertAsync(tablePrice);
						logger.LogDebug($"Prices for {prices.Instrument} published to Azure table {tableName}");
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
					pricesQueue.Enqueue(price);
				}
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
