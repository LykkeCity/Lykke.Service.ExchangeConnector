using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public class LykkeLogLoggerProvider : ILoggerProvider
    {
        readonly string connectionString;

        public LykkeLogLoggerProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new LykkeLogger(categoryName, connectionString);
        }

        public void Dispose()
        {
        }
    }
}
