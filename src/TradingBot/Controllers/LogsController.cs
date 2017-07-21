using System;
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
        public async Task<IActionResult> Index()
        {
            var config = Configuration.Instance;
            
                
            var logsStorage = new AzureTableStorage<LogEntity>(
                config.AzureTable.StorageConnectionString,
                config.Logger.TableName,
                new LogToConsole());
            
            var now = DateTime.UtcNow;
            var timePoint = now.AddMinutes(-3);
            var rowKey = timePoint.ToString("yyyy-MM-dd HH:mm:ss");

            var query = new TableQuery<LogEntity>();
                //.Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, rowKey));

            IEnumerable<LogEntity> logs = Enumerable.Empty<LogEntity>();
            await logsStorage.ExecuteAsync(query, result => logs = result);

            var lastEntries = logs.OrderByDescending(x => x.Timestamp).Take(100);
                
            return View(lastEntries);
        }
    }
}