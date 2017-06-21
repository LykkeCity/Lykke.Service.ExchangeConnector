using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.IO;
using TradingBot.Infrastructure.Configuration;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
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
	            var config = GetConfig(args);

                if (config.Logger.Enabled)
                    Logging.LoggerFactory.AddLykkeLog(config.AzureTable.StorageConnectionString, config.Logger.TableName);

				var cycle = new GetPricesCycle(config);
				var task = cycle.Start();
                var ctSource = new CancellationTokenSource();

				Logger.LogInformation("Press Ctrl+C for exit");

	            var host = new WebHostBuilder()
		            .UseKestrel()
		            .UseContentRoot(Directory.GetCurrentDirectory())
		            //.UseIISIntegration()
		            .UseStartup<Startup>()
		            //.UseUrls("*:5000")
		            .Build();

	            host.Run();
	            
//				Console.CancelKeyPress += (sender, eventArgs) =>
//					{
//						eventArgs.Cancel = true;
//
//						ctSource.Cancel();
//					    cycle.Stop();
//
//						if (task.Status == TaskStatus.Running)
//						{
//							Logger.LogInformation("Waiting for prices cycle completion");
//							task.Wait();
//						}
//					};
//
//
//                task.Wait(ctSource.Token);

				Logger.LogInformation("Applicatoin stopped.");
				Environment.Exit(0);
            }
            catch(Exception e)
            {
                Logger.LogError(new EventId(), e, "Application error");
                Environment.Exit(-1);
            }
        }
	
	    private static Configuration GetConfig(string[] args)
	    {
		    var configBuilder = new ConfigurationBuilder();
	            
	            
		    string settingsUrl = Environment.GetEnvironmentVariable("SettingsUrl");

		    if (string.IsNullOrEmpty(settingsUrl))
		    {
			    Logger.LogInformation("Empty SettingsUrl environment variable. Apply settings from appsettings.json file.");

			    configBuilder
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", optional: true)
					.AddCommandLine(args);
		    }
		    else
		    {
		        Logger.LogInformation("Apply settings from SettingsUrl");
			    
			    configBuilder
				    .AddJsonFile(new LykkeSettingsFileProvider(), path: settingsUrl, optional: false, reloadOnChange: false);    
		    }
	            
		    Configuration config = Configuration.FromConfigurationRoot(configBuilder.Build());

		    return config;
	    }
    }
}
