using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public class LykkeLogLoggerProvider : ILoggerProvider
    {
        private readonly string connectionString;
        private readonly string tableName;

        public LykkeLogLoggerProvider(string connectionString, string tableName)
        {
            this.connectionString = connectionString;
            this.tableName = tableName;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new LykkeLogger(categoryName, connectionString, tableName);
        }

        public void Dispose()
        {
        }
    }
}
