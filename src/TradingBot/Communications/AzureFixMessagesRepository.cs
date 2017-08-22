using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Microsoft.Extensions.Logging;
using QuickFix;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Communications
{
    public class AzureFixMessagesRepository
    {
        private readonly ILogger logger = Logging.CreateLogger<AzureFixMessagesRepository>();

        private readonly INoSQLTableStorage<FixMessageTableEntity> tableStorage;
   
        public AzureFixMessagesRepository(string connectionString, string tableName)
        {
            tableStorage = new AzureTableStorage<FixMessageTableEntity>(connectionString,
                tableName,
                new LogToConsole());
        }

        public void SaveMessage(Message message, FixMessageDirection direction)
        {
            SaveMessageAsync(message, direction).Wait();
		}

        public async Task SaveMessageAsync(Message message, FixMessageDirection direction)
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

                await tableStorage.InsertAsync(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(0, ex, "Can't save FIX message into azure storage");
            }
        }
    }
}