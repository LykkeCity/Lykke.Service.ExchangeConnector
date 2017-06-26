using System;
using System.Collections.Generic;
using System.Linq;
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
	        this.partitionKey = RemoveUnsupportedCharacters(instrument.Name);

			tableStorage = new AzureTableStorage<PriceTableEntity>(connectionString,
													   tableName,
													   new LogToConsole());
        }

	    /// <summary>
	    /// List of unsupported characters is from 
	    /// https://docs.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model
	    /// </summary>
	    private string RemoveUnsupportedCharacters(string name)
	    {
		    return RemoveCharacters(new[] { 
			    "/", "\\", "#", "?", "-",
			    
			    // Control characters from U+0000 to U+001F, including \t, \n, \r:
			    "\u0000", "\u0001", "\u0002", "\u0003", "\u0004", "\u0005", "\u0006", "\u0007", 
			    "\u0008", "\u0009", "\u000A", "\u000B", "\u000C", "\u000D", "\u000E", "\u000F", 
			    "\u0010", "\u0011", "\u0012", "\u0013", "\u0014", "\u0015", "\u0016", "\u0017", 
			    "\u0018", "\u0019", "\u001A", "\u001B", "\u001C", "\u001D", "\u001E", "\u001F", 
			    
			    // Control characters from U+007F to U+009F:
			    "\u007F", 
			    "\u0080", "\u0081", "\u0082", "\u0083", "\u0084", "\u0085", "\u0086", "\u0087", 
			    "\u0088", "\u0089", "\u008A", "\u008B", "\u008C", "\u008D", "\u008E", "\u008F", 
			    "\u0090", "\u0091", "\u0092", "\u0093", "\u0094", "\u0095", "\u0096", "\u0097", 
			    "\u0098", "\u0099", "\u009A", "\u009B", "\u009C", "\u009D", "\u009E", "\u009F",  
		    }, name);
	    }
	    
	    private string RemoveCharacters(string[] charactersToRemove, string str)
	    {
		    return charactersToRemove.Aggregate(str, (current, character) => current.Replace(character, string.Empty));
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
	    /// One serialized entry has size no more then 100 characters.
	    /// 32k / 100 = 400
	    /// </summary>
	    private const int MaxQueueCount = 320;
    }
}
