using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;
using Newtonsoft.Json;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using System.Linq;
using System.Text;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using System.Collections.Generic;
using TradingBot.Helpers;
using TradingBot.Common.Trading;
using TradingBot.Communications;

namespace TradingBot
{
    public class GetPricesCycle
    {
        private readonly ILogger Logger = Logging.CreateLogger<GetPricesCycle>();

        public GetPricesCycle(Configuration config)
        {
            this.config = config;

			exchange = ExchangeFactory.CreateExchange(config.ExchangeConfig);
            instruments = config.ExchangeConfig.Instruments.Select(x => new Instrument(x)).ToArray();
        }


        private readonly Exchange exchange;
		
        private readonly Instrument[] instruments;

		private CancellationTokenSource ctSource;

        private Configuration config;

        private RabbitMqPublisher<string> rabbitPublisher;

        private Dictionary<Instrument, AzureTablePricesPublisher> azurePublishers;

        public async Task Start()
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            Logger.LogInformation($"Price cycle starting for exchange {exchange.Name}...");

            bool connectionTestPassed = await new Reconnector(times: 5, pause: TimeSpan.FromSeconds(10))
                .ConnectAsync(exchange.TestConnection, token);

            if (!connectionTestPassed)
            {
                Logger.LogError($"Price cycle not started: no connection to exchange {exchange.Name}");
                return;
            }

            if (config.RabbitMQConfig.Enabled)
            {

                var rabbitSettings = new RabbitMqPublisherSettings()
                {
                    ConnectionString = config.RabbitMQConfig.Host,
                    ExchangeName = config.RabbitMQConfig.ExchangeName
                };
                
                var rabbitConsole = new RabbitConsole();
                
                rabbitPublisher = new RabbitMqPublisher<string>(rabbitSettings)
                    .SetSerializer(new MessageSerializer())
                    .SetLogger(new LogToConsole())
                    .SetPublishStrategy(new DefaultFnoutPublishStrategy())
                    .SetConsole(rabbitConsole)
                    .Start();
            }

            if (config.AzureTableConfig.Enabled)
            {
                azurePublishers = instruments.ToDictionary(
	                x => x,
	                x => new AzureTablePricesPublisher(x, config.AzureTableConfig.TableName, config.AzureTableConfig.StorageConnectionString));
            }


            var task = exchange.OpenPricesStream(instruments, PublishTickPrices);

            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
				Logger.LogDebug($"GetPricesCycle Heartbeat: {DateTime.Now}");
			}

			if (task.Status == TaskStatus.Running)
			{
				task.Wait();
			}
        }

        private async void PublishTickPrices(InstrumentTickPrices prices)
        {
            Logger.LogDebug($"{DateTime.Now}. {prices.TickPrices.Length} prices received for: {prices.Instrument}");

			if (config.RabbitMQConfig.Enabled)
			{
                string message = JsonConvert.SerializeObject(prices); // TODO: make serializator
			    await rabbitPublisher.ProduceAsync(message);
			}

            if (config.AzureTableConfig.Enabled)
            {
                await azurePublishers[prices.Instrument].Publish(prices);
            }
        }


        public void Stop()
        {
            Logger.LogInformation("Stop requested");
            ctSource.Cancel();

            exchange.ClosePricesStream();

            ((IStopable) rabbitPublisher)?.Stop();
        }

        public class MessageSerializer : IRabbitMqSerializer<string>
        {
            public byte[] Serialize(string model)
            {
                return Encoding.UTF8.GetBytes(model);
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
}
