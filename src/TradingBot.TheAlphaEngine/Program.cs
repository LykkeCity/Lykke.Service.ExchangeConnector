using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Configuration;
using TradingBot.Common.Infrastructure;
using Newtonsoft.Json;
using TradingBot.Common.Trading;
using TradingBot.TheAlphaEngine.Configuration;
using TradingBot.TheAlphaEngine.TradingAlgorithms;
using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngine;
using TradingBot.TheAlphaEngine.TradingAlgorithms.AlphaEngineJavaPort;

namespace TradingBot.TheAlphaEngine
{
    class Program
    {
		private static readonly ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
	        Logger.LogInformation("The Alpha Engine, version 0.1.0");

	        var config = GetConfig(args);
	        
            var ctSource = new CancellationTokenSource();
            var token = ctSource.Token;


	        var engine = CreateEngine(config.Algorithm);
	        
	        var rabbitSettings = new RabbitMqSubscriberSettings()
	        {
		        ConnectionString = config.RabbitMq.Host,
		        ExchangeName = config.RabbitMq.ExchangeName,
		        QueueName = config.RabbitMq.QueueName
	        };

	        var rabbit = new RabbitMqSubscriber<string>(rabbitSettings)
		        .SetMessageDeserializer(new DefaultStringDeserializer())
		        .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
	        	//.SetConsole(new RabbitConsole())
	        	.SetLogger(new LogToConsole())
		        .Subscribe(serialized =>
					{
						Logger.LogDebug("Alpha Engine has received data!");
						
						var prices = JsonConvert.DeserializeObject<TickPrice[]>(serialized);
	
						Logger.LogDebug($"Received {prices.Length} prices");
	
						engine.OnPriceChange(prices);
	
						return Task.FromResult(0);
					})
		        .Start();
	        
			Logger.LogInformation("Press Ctrl+C for exit");

			Console.CancelKeyPress += (sender, eventArgs) =>
				{
                    eventArgs.Cancel = true;
					((IStopable)rabbit).Stop();
                    ctSource.Cancel();
				};

	        while (!token.IsCancellationRequested)
	        {
		        Task.Delay(TimeSpan.FromSeconds(5), token).Wait();
		        Logger.LogDebug($"AlphaEngine Heartbeat: {DateTime.Now}");
	        }

			Console.WriteLine("Applicatoin stopped.");
			Environment.Exit(0);
        }
	    
	    private static Configuration.Configuration GetConfig(string[] args)
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
//			    Logger.LogInformation("Apply settings from SettingsUrl");
//			    
//			    configBuilder
//				    .AddJsonFile(new LykkeSettingsFileProvider(), path: settingsUrl, optional: false, reloadOnChange: false);    
		    }

		    Configuration.Configuration config = Configuration.Configuration.FromConfigurationRoot(configBuilder.Build());

		    return config;
	    }

	    private static ITradingAgent CreateEngine(AlgorithmConfiguration config)
	    {
		    switch (config.Implementation)
		    {
			    case AlgorithmImplementation.Stub:
				    return new StubTradingAgent();
				    break;
			    case AlgorithmImplementation.AlphaEngine:
				    return new AlphaEngineAgent(new Instrument(""));
				    break;
			    case AlgorithmImplementation.AlphaEngineJavaPort:
				    return new AlphaEngine("");
				    break;
			    default:
				    throw new ArgumentOutOfRangeException();
		    }
	    }
    }

	public class RabbitConsole : IConsole
	{
		public void WriteLine(string line)
		{
			Console.WriteLine(line);
		}
	}
}
