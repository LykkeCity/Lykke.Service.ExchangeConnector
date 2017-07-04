using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using System.Linq;
using System.Text;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using System.Collections.Generic;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;
using TradingBot.Common.Trading;
using TradingBot.Communications;
using TradingBot.Infrastructure.Logging;

namespace TradingBot
{
    public class GetPricesCycle
    {
        private readonly ILogger logger = Logging.CreateLogger<GetPricesCycle>();

        public GetPricesCycle(Configuration config)
        {
            this.config = config;

			exchange = ExchangeFactory.CreateExchange(config.Exchanges);
        }


        private readonly Exchange exchange;

		private CancellationTokenSource ctSource;

        private readonly Configuration config;

        private RabbitMqPublisher<InstrumentTickPrices> rabbitPublisher;

        private Dictionary<Instrument, AzureTablePricesPublisher> azurePublishers;


        private RabbitMqSubscriber<TradingSignal[]> signalSubscriber;
        

        public async Task Start()
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            if (exchange == null)
            {
                logger.LogInformation("There is no enabled exchange.");
                return;
            }
            
            UpdateAssetsTable();
            
            logger.LogInformation($"Price cycle starting for exchange {exchange.Name}...");

            var retry = Policy
                .HandleResult<bool>(x => !x)
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(10));
            
            bool connectionTestPassed = await retry.ExecuteAsync(exchange.TestConnection, token); 
                
            if (!connectionTestPassed)
            {
                logger.LogError($"Price cycle not started: no connection to exchange {exchange.Name}");
                return;
            }

            if (config.RabbitMq.Enabled)
            {

                var publisherSettings = new RabbitMqPublisherSettings()
                {
                    ConnectionString = config.RabbitMq.GetConnectionString(),
                    ExchangeName = config.RabbitMq.RatesExchange
                };
                
                var rabbitConsole = new RabbitConsole();
                
                rabbitPublisher = new RabbitMqPublisher<InstrumentTickPrices>(publisherSettings)
                    .SetSerializer(new InstrumentTickPricesConverter())
                    .SetLogger(new LogToConsole())
                    .SetPublishStrategy(new DefaultFnoutPublishStrategy())
                    .SetConsole(rabbitConsole)
                    .Start();
                
                
                var subscriberSettings = new RabbitMqSubscriberSettings()
                {
                    ConnectionString = config.RabbitMq.GetConnectionString(),
                    ExchangeName = config.RabbitMq.SignalsExchange,
                    QueueName = config.RabbitMq.QueueName
                };
                
                signalSubscriber = new RabbitMqSubscriber<TradingSignal[]>(subscriberSettings)
                    .SetMessageDeserializer(new TradingSignalsConverter())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .SetConsole(rabbitConsole)
                    .SetLogger(new LogToConsole())
                    .Subscribe(TradingSignalHandler)
                    .Start();                
            }

            if (config.AzureTable.Enabled)
            {
                azurePublishers = exchange.Instruments.ToDictionary(
	                x => x,
	                x => new AzureTablePricesPublisher(x, config.AzureTable.TableName, config.AzureTable.StorageConnectionString));
            }


            var task = exchange.OpenPricesStream(PublishTickPrices);

            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
				logger.LogDebug($"GetPricesCycle Heartbeat: {DateTime.Now}");
			}

			if (task.Status == TaskStatus.Running)
			{
				task.Wait();
			}
        }

        private async void UpdateAssetsTable()
        {
            if (!config.AzureTable.Enabled)
                return;

            try
            {
                logger.LogInformation($"Updating Assets table...");
            
                INoSQLTableStorage<TableEntity> tableStorage = new AzureTableStorage<TableEntity>(
                    config.AzureTable.StorageConnectionString, 
                    config.AzureTable.AssetsTableName, 
                    new LogToConsole());

                var exchangeName = exchange.Name;

                await tableStorage.InsertOrReplaceBatchAsync(
                    exchange.Instruments.Select(x => new TableEntity(exchangeName.GetAzureFriendlyName(), x.Name.GetAzureFriendlyName())));
            }
            catch (Exception e)
            {
                logger.LogError(new EventId(), e, "Can't update Assets table");
            }
        }

        private async void PublishTickPrices(InstrumentTickPrices prices)
        {
            //logger.LogDebug($"{DateTime.Now}. {prices.TickPrices.Length} prices received for: {prices.Instrument}");

			if (config.RabbitMq.Enabled)
			{
			    await rabbitPublisher.ProduceAsync(prices);
			}

            if (config.AzureTable.Enabled)
            {
                await azurePublishers[prices.Instrument].Publish(prices);
            }
        }


        public void Stop()
        {
            logger.LogInformation("Stop requested");
            ctSource.Cancel();

            exchange?.ClosePricesStream();

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

        private Task TradingSignalHandler(TradingSignal[] signals)
        {
            logger.LogDebug($"{signals.Length} trading signals: {string.Join(", ", signals.Select(x => x.ToString()))}");
            
            // TODO: execute the signals

            return Task.FromResult(0);
        }
    }
}
