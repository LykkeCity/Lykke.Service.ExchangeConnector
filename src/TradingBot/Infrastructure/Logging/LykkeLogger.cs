using System;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using Lykke.SlackNotifications;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.Logging
{
    public class LykkeLogger : ILogger
    {
        private readonly ILog lykkeLogger;

        public LykkeLogger(string applicationName, string connectionString, 
                           ISlackNotificationsSender slackNotificationsSender = null,
                           string tableName = "Logs")
        {
			//var applicationName1 =
	        //    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName;

            lykkeLogger = new LykkeLogToAzureStorage(
				applicationName,
				new AzureTableStorage<LogEntity>(connectionString, tableName, null),
				slackNotificationsSender);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    lykkeLogger.WriteFatalErrorAsync(component: "", process: "", context: "", exeption: exception);
                    break;

				case LogLevel.Warning:
                    lykkeLogger.WriteWarningAsync(component: "", process: "", context: "", info: state.ToString());
                    break;

				case LogLevel.Information:
				case LogLevel.Debug:
				case LogLevel.Trace:
                    lykkeLogger.WriteInfoAsync(component: "", process: "", context: "", info: state.ToString());
                    break;

                case LogLevel.Error:
                    lykkeLogger.WriteErrorAsync(component: "", process: "", context: "", exeption: exception);
                    break;

                case LogLevel.None:
                    break;
            }
        }
    }
}
