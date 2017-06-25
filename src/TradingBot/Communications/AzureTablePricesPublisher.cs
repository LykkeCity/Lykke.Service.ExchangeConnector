using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
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


        public AzureTablePricesPublisher(Instrument instrument, string tableName, string connectionString)
        {
            this.instrument = instrument;

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

				if (timeMunite > currentPriceMinute)
				{
					var tablePrice = new PriceTableEntity(instrument.Name,
															currentPriceMinute,
															pricesQueue.ToArray());
														
					pricesQueue.Clear();
					pricesQueue.Enqueue(price);

					try
					{
						currentPriceMinute = timeMunite;
						await tableStorage.InsertAsync(tablePrice);
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
					
					// TODO: check queue size. One field should be less then 64kb
				}
			}
		}
	}
}
