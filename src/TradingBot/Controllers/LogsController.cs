using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Communications;
using TradingBot.Repositories;

namespace TradingBot.Controllers
{
    public class LogsController : Controller
    {
        private readonly ExchangeConnectorApplication _app;
        private readonly int entriesCount = 50;
        private readonly INoSQLTableStorage<LogEntity> _logsStorage;
        private readonly INoSQLTableStorage<JavaLogEntity> _javaLogsStorage;
        private readonly INoSQLTableStorage<FixMessageTableEntity> _fixMessagesStorage;
        private readonly INoSQLTableStorage<JavaIntrinsicEventEntity> _javaEventsStorage;

        public LogsController(
            ExchangeConnectorApplication app,
            INoSQLTableStorage<LogEntity> logsStorage,
            INoSQLTableStorage<JavaLogEntity> javaLogsStorage,
            INoSQLTableStorage<FixMessageTableEntity> fixMessagesStorage,
            INoSQLTableStorage<JavaIntrinsicEventEntity> javaEventsStorage)
        {
            _app = app;
            _logsStorage = logsStorage;
            _javaLogsStorage = javaLogsStorage;
            _fixMessagesStorage = fixMessagesStorage;
            _javaEventsStorage = javaEventsStorage;
        }

        public async Task<IActionResult> Index()
        {
            var query = new TableQuery<LogEntity>()
            {
                TakeCount = entriesCount
            };
            
            var logs = new List<LogEntity>(entriesCount);
            await _logsStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderByDescending(x => x.Timestamp);
                
            return View(lastEntries);
        }

        public async Task<IActionResult> AlphaEngine()
        {
            var query = new TableQuery<JavaLogEntity>()
            {
                TakeCount = entriesCount
            };

            var logs = new List<JavaLogEntity>(entriesCount);
            await _javaLogsStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderByDescending(x => x.Timestamp);

            return View(lastEntries);
        }

        public async Task<IActionResult> Fix()
        {
            var query = new TableQuery<FixMessageTableEntity>
            {
                TakeCount = entriesCount
            };

            var logs = new List<FixMessageTableEntity>(entriesCount);
            await _fixMessagesStorage.ExecuteAsync(query, result => logs.AddRange(result));

            var lastEntries = logs.OrderBy(x => x.RowKey);

            return View(lastEntries);
        }

        public async Task<IActionResult> TranslatedSignals()
        {
            return View(await _app.TranslatedSignalsRepository.GetTop(entriesCount));
        }

        public async Task<IActionResult> IntrinsicEvents()
        {
            return View(await _javaEventsStorage.GetTopRecordsAsync("intrinsicEvent", entriesCount));
        }
    }
}
