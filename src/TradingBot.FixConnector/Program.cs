using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickFix;
using TradingBot.Common.Infrastructure;
using TradingBot.FixConnector.Configuration;

namespace TradingBot.FixConnector
{
    class Program
    {
        private static readonly ILogger logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            var config = new ConnectionConfig();
            var settings = new SessionSettings(Directory.GetCurrentDirectory() +  "/fix.config");


            try
            {
				logger.LogInformation("Trying to connect..");
				var ctSource = new CancellationTokenSource();
				var server = new Server();
				var task = server.StartAsync(config, settings, ctSource.Token);

				
				Console.CancelKeyPress += (sender, eventArgs) =>
					{
						eventArgs.Cancel = true;

						ctSource.Cancel();

						if (task.Status == TaskStatus.Running)
						{
							logger.LogInformation("Waiting for prices cycle completion");
							task.Wait();
						}
					};

				logger.LogInformation("Press Ctrl+C for exit");

				//task.Wait();
                while (!ctSource.IsCancellationRequested)
                {
                    Task.Delay(TimeSpan.FromMinutes(1), ctSource.Token).Wait();
                }

			}
            catch (Exception ex)
            {
                logger.LogError(new EventId(), ex, "Error during task execution");
            }
            finally 
            {
				logger.LogInformation("Applicatoin stopped.");
				Environment.Exit(0);    
            }
        }
    }
}
