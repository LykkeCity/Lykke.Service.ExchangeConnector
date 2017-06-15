using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.IO;
using TradingBot.Infrastructure.Configuration;
using System.Threading;
using TradingBot.Infrastructure.Logging;

namespace TradingBot
{
    class Program
    {
        private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            try
            {
				var configBuilder = new ConfigurationBuilder();
				configBuilder
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true)
					.AddCommandLine(args);

				Configuration config = Configuration.FromConfigurationRoot(configBuilder.Build());


                Logging.LoggerFactory
                       .AddLykkeLog(config.CommonConfig.LoggerStorageConnectionString);

				var cycle = new GetPricesCycle(config);
				var task = cycle.Start();
                var ctSource = new CancellationTokenSource();

				Logger.LogInformation("Press Ctrl+C for exit");

				Console.CancelKeyPress += (sender, eventArgs) =>
					{
						eventArgs.Cancel = true; // Don't terminate the process immediately, wait for the Main thread to exit gracefully.

						ctSource.Cancel();
					    cycle.Stop();

						if (task.Status == TaskStatus.Running)
						{
							Logger.LogInformation("Waiting for prices cycle completion");
							task.Wait();
						}
					};


                task.Wait(ctSource.Token);

				Logger.LogInformation("Applicatoin stopped.");
				Environment.Exit(0);
            }
            catch(Exception e)
            {
                Logger.LogError(new EventId(), e, "Application error");
                Environment.Exit(-1);
            }
        }
    }
}
