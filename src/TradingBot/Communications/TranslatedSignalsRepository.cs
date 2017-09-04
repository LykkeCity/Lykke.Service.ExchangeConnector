using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Microsoft.Extensions.Logging;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Communications
{
    public class TranslatedSignalsRepository
    {     
        private readonly ILogger logger = Logging.CreateLogger<TranslatedSignalsRepository>();

        private readonly INoSQLTableStorage<TranslatedSignalTableEntity> tableStorage;

        private readonly InverseDateTimeRowKeyProvider keyProvider;
        
        public TranslatedSignalsRepository(string connectionString, string tableName, InverseDateTimeRowKeyProvider keyProvider)
        {
            this.keyProvider = keyProvider;
            
            tableStorage = new AzureTableStorage<TranslatedSignalTableEntity>(
                connectionString,
                tableName,
                new LogToConsole());
        }
        
        public void Save(TranslatedSignalTableEntity translatedSignal)
        {
            SaveAsync(translatedSignal).Wait();
        }

        public async Task SaveAsync(TranslatedSignalTableEntity translatedSignal)
        {
            try
            {
                translatedSignal.PartitionKey = "nopartition";
                translatedSignal.RowKey = keyProvider.GetNextRowKey().ToString();
                
                await tableStorage.InsertAsync(translatedSignal); // TODO: save by batchs
            }
            catch (Exception ex)
            {
                logger.LogError(0, ex, "Can't save translated signal into azure storage");
            }
        }

        public Task<IEnumerable<TranslatedSignalTableEntity>> GetTop(int count)
        {
            return tableStorage.GetTopRecordsAsync("nopartition", count);
        }
    }
}