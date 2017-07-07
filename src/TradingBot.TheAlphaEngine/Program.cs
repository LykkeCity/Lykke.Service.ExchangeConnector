using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Configuration;
using TradingBot.Common.Infrastructure;
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


	    private static RabbitMqPublisher<InstrumentTradingSignals> rabbitPublisher;
	    private static RabbitMqSubscriber<InstrumentTickPrices> rabbitSubscriber;
	    
        static void Main(string[] args)
        {
	        Logger.LogInformation("The Alpha Engine, version 0.1.0");

	        var config = GetConfig(args);
	        
            var ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

	        var engine = CreateEngine(config.Algorithm);

	        Logger.LogDebug("Waiting a bit for services up...");
	        Task.Delay(TimeSpan.FromSeconds(10)).Wait();
	        
	        SetupPublishSignals(config, engine);
	        SubscribeToPrices(config, engine);

	        
			Logger.LogInformation("Press Ctrl+C for exit");

			Console.CancelKeyPress += (sender, eventArgs) =>
				{
                    eventArgs.Cancel = true;
					
					((IStopable)rabbitSubscriber)?.Stop();
					((IStopable)rabbitPublisher)?.Stop();
					
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

	    private static void SetupPublishSignals(Configuration.Configuration config, ITradingAgent agent)
	    {
		    var rabbitSettings = new RabbitMqPublisherSettings()
		    {
			    ConnectionString = config.RabbitMq.GetConnectionString(),
			    ExchangeName = config.RabbitMq.SignalsExchange
		    };
		    
		    rabbitPublisher = new RabbitMqPublisher<InstrumentTradingSignals>(rabbitSettings)
			    .SetSerializer(new InstrumentTradingSignalsConverter())
			    .SetPublishStrategy(new DefaultFnoutPublishStrategy())
			    .SetLogger(new LogToConsole())
			    .Start();

		    agent.TradingSignalGenerated += PublishTradingSignalToRabbit;
	    }
	    
	    private static void SubscribeToPrices(Configuration.Configuration config, ITradingAgent agent)
	    {
		    var rabbitSettings = new RabbitMqSubscriberSettings()
		    {
			    ConnectionString = config.RabbitMq.GetConnectionString(),
			    ExchangeName = config.RabbitMq.RatesExchange,
			    QueueName = config.RabbitMq.QueueName
		    };

		    rabbitSubscriber = new RabbitMqSubscriber<InstrumentTickPrices>(rabbitSettings)
			    .SetMessageDeserializer(new InstrumentTickPricesConverter())
			    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
			    //.SetConsole(new RabbitConsole())
			    .SetLogger(new LogToConsole())
			    .Subscribe(prices =>
			    {
				    Logger.LogDebug("Alpha Engine has received data!");
						
				    Logger.LogDebug($"Received {prices.TickPrices.Length} prices");
	
				    agent.OnPriceChange(prices.TickPrices);
	
				    return Task.FromResult(0);
			    })
			    .Start();
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

	    private static void PublishTradingSignalToRabbit(TradingSignal signal)
	    {
		    Logger.LogDebug($"New signal been generated: {signal}");
		    rabbitPublisher.ProduceAsync(new InstrumentTradingSignals(new Instrument(""), new [] { signal }));
		    Logger.LogDebug("Signal published to rabbit");
	    }

	    private static ITradingAgent CreateEngine(AlgorithmConfiguration config)
	    {
		    switch (config.Implementation)
		    {
			    case AlgorithmImplementation.Stub:
				    return new StubTradingAgent();
				    break;
			    case AlgorithmImplementation.AlphaEngine:
				    return new AlphaEngineAgent(new Instrument(""), config.InitialPosition);
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
