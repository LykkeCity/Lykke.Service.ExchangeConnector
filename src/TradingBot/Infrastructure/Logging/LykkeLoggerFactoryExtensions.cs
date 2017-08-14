using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public static class LykkeLoggerFactoryExtensions
    {
		public static ILoggerFactory AddLykkeLog(this ILoggerFactory factory, string connectionString, string tableName = "Logs")
		{
            factory.AddProvider(new LykkeLogLoggerProvider(connectionString, tableName));
			return factory;
		}
    }
}
