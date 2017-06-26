using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
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
            var pricesStorage = new AzureTableStorage<PriceTableEntity>(config.AzureTable.StorageConnectionString,
                config.AzureTable.TableName,
                new LogToConsole());

            var now = DateTimeOffset.UtcNow;
            var timePoint = now.AddMinutes(-1);

            var prices = await pricesStorage.GetDataAsync(x => x.Timestamp >= timePoint);

            var lastPrices = prices.OrderByDescending(x => x.Timestamp).Take(10);
            
            return new StatusReport()
            {
                LastPrices = lastPrices.ToList()
            };
        }
    }
}