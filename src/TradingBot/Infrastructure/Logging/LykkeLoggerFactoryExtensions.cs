using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public static class LykkeLoggerFactoryExtensions
    {
		public static ILoggerFactory AddLykkeLog(this ILoggerFactory factory, string connectionString)
		{
            factory.AddProvider(new LykkeLogLoggerProvider(connectionString));
			return factory;
		}
    }
}
