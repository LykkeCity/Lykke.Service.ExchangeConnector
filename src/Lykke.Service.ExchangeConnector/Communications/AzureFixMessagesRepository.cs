using System;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using QuickFix;
using ILog = Common.Log.ILog;

namespace TradingBot.Communications
{
    internal sealed class AzureFixMessagesRepository : IAzureFixMessagesRepository
    {
        private readonly INoSQLTableStorage<FixMessageTableEntity> _tableStorage;
        private readonly ILog _log;

        public AzureFixMessagesRepository(INoSQLTableStorage<FixMessageTableEntity> tableStorage, ILog log)
        {
            _tableStorage = tableStorage;
            _log = log.CreateComponentScope(nameof(AzureFixMessagesRepository));
        }

        public void SaveMessage(Message message, FixMessageDirection direction)
        {
            SaveMessageAsync(message, direction).GetAwaiter().GetResult();
        }

        private async Task SaveMessageAsync(Message message, FixMessageDirection direction)
        {
            try
            {
                var entity = new FixMessageTableEntity()
                {
                    PartitionKey = "icm", // TODO
                    RowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString(),
                    Type = message.GetType().Name,
                    Message = message.ToString().Replace("\u0001", "^"),
                    Direction = direction
                };

                await _tableStorage.InsertAsync(entity);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(SaveMessageAsync), string.Empty, "Saving fix messages", ex);
            }
        }
    }
}
