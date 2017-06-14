using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Configuration;
//using TradingBot.Common.Communications;
using Newtonsoft.Json;
using TradingBot.Common.Trading;

namespace TradingBot.TheAlphaEngine
{
    class Program
    {
		private static ILogger Logger = Logging.CreateLogger<Program>();

        static void Main(string[] args)
        {
	        Logger.LogInformation("The Alpha Engine, version 0.1.0");
	        
			var configBuilder = new ConfigurationBuilder();
			configBuilder
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true)
				.AddCommandLine(args);

            var config = configBuilder.Build();

            var rabbitConfig = config.GetSection("rabbitMQ").Get<RabbitMQConfiguration>();

            var ctSource = new CancellationTokenSource();
            var token = ctSource.Token;


            var engine = new AlphaEngine("");
	        
	        var rabbitSettings = new RabbitMqSubscriberSettings()
	        {
		        ConnectionString = rabbitConfig.Host,
		        ExchangeName = rabbitConfig.ExchangeName,
		        QueueName = rabbitConfig.QueueName
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
	
						engine.OnPriceChanged(prices);
	
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
    }

	public class RabbitConsole : IConsole
	{
		public void WriteLine(string line)
		{
			Console.WriteLine(line);
		}
	}
}
