using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Microsoft.WindowsAzure.Storage.Table;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot
{
    public class StatusReport
    {
        public bool Ok { get; set; }
        
        public string LastLogEntry { get; set; }
        
        public List<PriceTableEntity> LastPrices { get; set; }
        
        public static Task<StatusReport> Create()
        {
            return Create(Configuration.Instance);
        }

        public static async Task<StatusReport> Create(Configuration config)
        {
//            var assetsStorage = new AzureTableStorage<TableEntity>(
//                config.AzureTable.StorageConnectionString,
//                config.AzureTable.AssetsTableName,
//                new LogToConsole());
//
//            var assets = await assetsStorage.GetDataAsync();
            
            var pricesStorage = new AzureTableStorage<PriceTableEntity>(
                config.AzureTable.StorageConnectionString,
                config.AzureTable.TableName,
                new LogToConsole());
            
            var now = DateTime.UtcNow;
            var timePoint = now.AddMinutes(-1);
            var rowKey = timePoint.ToString("yyyy-MM-dd HH:mm:ss");

            var query = new TableQuery<PriceTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, rowKey));

            IEnumerable<PriceTableEntity> prices = Enumerable.Empty<PriceTableEntity>();
            await pricesStorage.ExecuteAsync(query, result => prices = result);

            var lastPrices = prices.OrderByDescending(x => x.Timestamp).Take(10);
            
            return new StatusReport()
            {
                LastPrices = lastPrices.ToList()
            };
        }
    }
}