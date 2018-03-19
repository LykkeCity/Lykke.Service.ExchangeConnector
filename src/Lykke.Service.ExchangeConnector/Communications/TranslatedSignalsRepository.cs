using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using TradingBot.Repositories;

namespace TradingBot.Communications
{
    public class TranslatedSignalsRepository
    {

        private readonly INoSQLTableStorage<TranslatedSignalTableEntity> _tableStorage;

        private readonly InverseDateTimeRowKeyProvider keyProvider;
        private readonly ILog _log;

        public TranslatedSignalsRepository(INoSQLTableStorage<TranslatedSignalTableEntity> tableStorage, InverseDateTimeRowKeyProvider keyProvider, ILog log)
        {
            _tableStorage = tableStorage;
            this.keyProvider = keyProvider;
            _log = log;
        }

        public void Save(TranslatedSignalTableEntity translatedSignal)
        {
            SaveAsync(translatedSignal).Wait();
        }

        public async Task SaveAsync(TranslatedSignalTableEntity translatedSignal)
        {
            try
            {
                translatedSignal.PartitionKey = TranslatedSignalTableEntity.GeneratePartitionKey();
                translatedSignal.RowKey = keyProvider.GetNextRowKey().ToString();

                await _tableStorage.InsertAsync(translatedSignal); // TODO: save by batchs
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(SaveAsync), "Can't save translated signal into azure storage", ex);
            }
        }

        public Task<IEnumerable<TranslatedSignalTableEntity>> GetTop(int count)
        {
            return _tableStorage.GetTopRecordsAsync(TranslatedSignalTableEntity.GeneratePartitionKey(), count);
        }
    }
}
