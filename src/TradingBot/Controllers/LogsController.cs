using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Controllers
{
    public class LogsController : Controller
    {
        private Configuration Config => Configuration.Instance;
        
        private readonly int entriesCount = 100;
        
        public async Task<IActionResult> Index()
        {
            var logsStorage = new AzureTableStorage<LogEntity>(
                Config.AzureStorage.StorageConnectionString,
                Config.LogsTableName,
                new LogToConsole());
            
            var query = new TableQuery<LogEntity>()
            {
                TakeCount = entriesCount
            };
            
            var logs = new List<LogEntity>(entriesCount);
            await logsStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderByDescending(x => x.Timestamp);
                
            return View(lastEntries);
        }

        public async Task<IActionResult> AlphaEngine()
        {
            var logsStorage = new AzureTableStorage<JavaLogEntity>(
                Config.AzureStorage.StorageConnectionString,
                "logsAlphaEngine",
                new LogToConsole());
            
            var query = new TableQuery<JavaLogEntity>()
            {
                TakeCount = entriesCount
            };

            var logs = new List<JavaLogEntity>(entriesCount);
            await logsStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderByDescending(x => x.Timestamp);

            return View(lastEntries);
        }

        public async Task<IActionResult> Fix()
        {
            var logsStorage = new AzureTableStorage<FixMessageTableEntity>(
                Config.AzureStorage.StorageConnectionString,
                "fixMessages",
                new LogToConsole());

            var query = new TableQuery<FixMessageTableEntity>
            {
                TakeCount = entriesCount
            };

            var logs = new List<FixMessageTableEntity>(entriesCount);
            await logsStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderBy(x => x.RowKey);

            return View(lastEntries);
        }
    }

    public class JavaLogEntity : TableEntity
    {
        public JavaLogEntity()
        {
        }
        
        public string Message { get; set; }
        
        public string Level { get; set; }
    }
}