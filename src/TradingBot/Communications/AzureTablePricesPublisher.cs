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
		private readonly ILogger Logger = Logging.CreateLogger<AzureTablePricesPublisher>();

        private Instrument instrument;

        private INoSQLTableStorage<PriceTableEntity> tableStorage;


        public AzureTablePricesPublisher(Instrument instrument, string tableName, string connectionString)
        {
            this.instrument = instrument;

			tableStorage = new AzureTableStorage<PriceTableEntity>(connectionString,
													   tableName,
													   new LogToConsole());
        }


		private Queue<TickPrice> pricesQueue = new Queue<TickPrice>();
		private Queue<PriceTableEntity> tablePricesQueue = new Queue<PriceTableEntity>();
		private DateTime currentPriceMinute;

		public async Task Publish(InstrumentTickPrices prices)
		{
			if (currentPriceMinute == default(DateTime))
                currentPriceMinute = prices.TickPrices[0].Time.TruncSeconds();

            foreach (var price in prices.TickPrices)
			{
				var timeWithoutSeconds = price.Time.TruncSeconds();

				if (timeWithoutSeconds > currentPriceMinute)
				{
					var tablePrice = new PriceTableEntity(instrument.Name,
														  currentPriceMinute,
														  pricesQueue.ToArray());
					pricesQueue.Clear();

					try
					{
						await tableStorage.InsertAsync(tablePrice);
					}
					catch (Microsoft.WindowsAzure.Storage.StorageException ex)
						when (ex.Message == "Conflict")
					{
						Logger.LogInformation($"Conflict on writing. Skip chunk for {currentPriceMinute}");
					}
					catch (Exception ex)
					{
						tablePricesQueue.Enqueue(tablePrice);
						Logger.LogError(new EventId(), ex, $"Can't write to Azure Table Storage, will try later. Now in queue: {tablePricesQueue.Count}");
					}

					currentPriceMinute = timeWithoutSeconds;
				}

				pricesQueue.Enqueue(price);
			}
		}
	}
}
