using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Application;
using Common.Log;
using Lykke.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Controllers
{
    public class LogsController : Controller
    {
        private Configuration Config => Configuration.Instance;
        
        private readonly int entriesCount = 50;
        
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


        public async Task<IActionResult> TranslatedSignals()
        {
            return View(await Program.Application.TranslatedSignalsRepository.GetTop(entriesCount));
        }

        public async Task<IActionResult> IntrinsicEvents()
        {
            var storage = new AzureTableStorage<JavaIntrinsicEventEntity>(
                Config.AzureStorage.StorageConnectionString,
                "intrinsicEvents",
                new LogToConsole());

            return View(await storage.GetTopRecordsAsync("intrinsicEvent", entriesCount));
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

    public class JavaIntrinsicEventEntity : TableEntity
    {
        public string Exchange { get; set; }
        public string Instrument { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Liquidity { get; set; }
        public string CoastLineTraderId { get; set; }
        public int ProperRunnerIndex { get; set; }
        public int Runner0event { get; set; }
        public int Runner1event { get; set; }
        public int Runner2event { get; set; }
        public double BuyLimitOrderPrice { get; set; }
        public double BuyLimitOrderVolume { get; set; }
        public string BuyLimitOrderId { get; set; }
        public double SellLimitOrderPrice { get; set; }
        public double SellLimitOrderVolume { get; set; }
        public string SellLimitOrderId { get; set; }
        public double Inventory { get; set; }
        public DateTime Datetime { get; set; }
        public int LongShort { get; set; }
    }
}