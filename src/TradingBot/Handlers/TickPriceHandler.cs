using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Common.Configuration;
using TradingBot.Common.Trading;
using TradingBot.Communications;
using Common.Log;
using TradingBot.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using AzureStorage;
using AzureStorage.Tables;
using Microsoft.WindowsAzure.Storage.Table;

namespace TradingBot.Handlers
{
    public class TickPricesAzurePublisher : Handler<InstrumentTickPrices>
    {
        private readonly ILogger logger = Logging.CreateLogger<TickPricesAzurePublisher>();

        private readonly Dictionary<Instrument, AzureTablePricesPublisher> azurePublishers;
        
        public TickPricesAzurePublisher(
            IEnumerable<Instrument> instruments,
            AzureTableConfiguration azureConfig)
        {
            azurePublishers = instruments.ToDictionary(
                x => x,
                x => new AzureTablePricesPublisher(x, azureConfig.TableName, azureConfig.StorageConnectionString));

            UpdateAssetsTable(azureConfig, instruments);
        }
        
        public override Task Handle(InstrumentTickPrices tickPrices)
        {
            return azurePublishers[tickPrices.Instrument].Publish(tickPrices);
        }

		private async void UpdateAssetsTable(AzureTableConfiguration azureConfig, 
			IEnumerable<Instrument> instruments)
		{
			try
			{
				logger.LogInformation($"Updating Assets table...");

				INoSQLTableStorage<TableEntity> tableStorage = new AzureTableStorage<TableEntity>(
					azureConfig.StorageConnectionString,
					azureConfig.AssetsTableName,
					new LogToConsole());

				await tableStorage.InsertOrReplaceBatchAsync(
					instruments.Select(x => new TableEntity(azureConfig.TableName, x.Name.GetAzureFriendlyName())));
			}
			catch (Exception e)
			{
				logger.LogError(new EventId(), e, "Can't update Assets table");
			}
		}
    }
}