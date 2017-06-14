using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Trading;
using Newtonsoft.Json;
using AzureStorage.Tables;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using AzureStorage;
using System.Linq;
using System.Globalization;
using System.Text;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using System.Collections.Generic;
using TradingBot.Helpers;
using TradingBot.Common.Trading;

namespace TradingBot
{
    public class GetPricesCycle
    {
        private readonly ILogger Logger = Logging.CreateLogger<GetPricesCycle>();

        public GetPricesCycle(Exchange exchange, Instrument[] instruments)
        {
            this.instruments = instruments ?? throw new ArgumentNullException(nameof(instruments));
            this.exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        }

        public GetPricesCycle(Configuration config)
        {
            this.config = config;

			exchange = ExchangeFactory.CreateExchange(config.ExchangeConfig);
			instruments = new[] { new Instrument(config.ExchangeConfig.Instrument) };
        }


        private readonly Exchange exchange;
		
        private readonly Instrument[] instruments;

		private CancellationTokenSource ctSource;

        private Configuration config;

        private RabbitMqPublisher<string> rabbitPublisher;

        private INoSQLTableStorage<PriceTableEntity> tableStorage;

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
                tableStorage = new AzureTableStorage<PriceTableEntity>(config.AzureTableConfig.StorageConnectionString, 
                                                                       config.AzureTableConfig.TableName, 
                                                                       new LogToConsole());
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

        private async void PublishTickPrices(TickPrice[] prices)
        {
            Logger.LogDebug($"{DateTime.Now}. Prices received: {prices.Length}");

			if (config.RabbitMQConfig.Enabled)
			{
                string message = JsonConvert.SerializeObject(prices); // TODO: make serializator
			    await rabbitPublisher.ProduceAsync(message);
			}

            if (config.AzureTableConfig.Enabled)
            {
                await PublishToAzureStorage(prices);
            }
        }


        private Queue<TickPrice> pricesQueue = new Queue<TickPrice>();
        private Queue<PriceTableEntity> tablePricesQueue = new Queue<PriceTableEntity>();
        private DateTime currentPriceMinute;

        private async Task PublishToAzureStorage(TickPrice[] prices)
        {
            if (currentPriceMinute == default(DateTime))
                currentPriceMinute = prices[0].Time.TruncSeconds();

            foreach (var price in prices)
            {
                var timeWithoutSeconds = price.Time.TruncSeconds();

                if (timeWithoutSeconds > currentPriceMinute)
                {
                    var tablePrice = new PriceTableEntity(instruments[0].Name,
                                                                  currentPriceMinute,
                                                                  pricesQueue.ToArray());
                    pricesQueue.Clear();

                    try
                    {
						await tableStorage.InsertAsync(tablePrice);
					}
                    catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                        when (ex.Message == "Conflict")
                    {
                        Logger.LogInformation($"Conflict on writing. Skip chank for {currentPriceMinute}");
                    }
                    catch (Exception ex)
                    {
                        tablePricesQueue.Enqueue(tablePrice);
                        Logger.LogError(new EventId(), ex, $"Can't write to Azure Table Storage, will try later. Now in queue: {tablePricesQueue.Count}");
                    }

                    currentPriceMinute = timeWithoutSeconds;
                }

				pricesQueue.Enqueue(price);
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
