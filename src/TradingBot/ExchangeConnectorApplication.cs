using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Polly;
using TradingBot.Communications;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot
{
    public class ExchangeConnectorApplication : IApplicationFacade
    {
        private readonly ILogger logger = Logging.CreateLogger<ExchangeConnectorApplication>();
        
        public TranslatedSignalsRepository TranslatedSignalsRepository { get; }

        public ExchangeConnectorApplication(Configuration config)
        {
            this.config = config;
            TranslatedSignalsRepository = new TranslatedSignalsRepository(config.AzureStorage.StorageConnectionString, "translatedSignals", new InverseDateTimeRowKeyProvider());
            
            exchanges = ExchangeFactory.CreateExchanges(config, TranslatedSignalsRepository).ToDictionary(x => x.Name, x => x);
        }

        private readonly Dictionary<string, Exchange> exchanges;
        
		private CancellationTokenSource ctSource;
        private readonly Configuration config;
        private RabbitMqSubscriber<InstrumentTradingSignals> signalSubscriber;

        public async Task Start()
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            if (!exchanges.Any())
            {
                logger.LogInformation("There is no enabled exchange.");
                return;
            }
            
            logger.LogInformation($"Price cycle starting for exchanges: {string.Join(", ", exchanges.Keys)}...");

            var retry = Policy
                .HandleResult<bool>(x => !x)
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(10));

            foreach (var exchange in exchanges.Values.ToList())
            {
                bool connectionTestPassed = await retry.ExecuteAsync(exchange.TestConnection, token);
                if (!connectionTestPassed)
                {
                    logger.LogWarning($"no connection to exchange {exchange.Name}");
                    exchanges.Remove(exchange.Name); // TODO: do not remove, just try to connect further
                }
            }
            
            if (config.RabbitMq.Enabled)
            {
                SetupTradingSignalsSubscription(config.RabbitMq);
            }

            var task = Task.WhenAll(exchanges.Values.Select(x => x.OpenPricesStream()));

            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
				logger.LogDebug($"GetPricesCycle Heartbeat: {DateTime.Now}");
			    // TODO: collect some stats to get health status
			}

			if (task.Status == TaskStatus.Running)
			{
				task.Wait();
			}
        }

        private void SetupTradingSignalsSubscription(RabbitMqConfiguration rabbitConfig)
        {
            var subscriberSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = rabbitConfig.GetConnectionString(),
                ExchangeName = rabbitConfig.SignalsExchange,
                QueueName = rabbitConfig.SignalsQueue
            };
            
            signalSubscriber = new RabbitMqSubscriber<InstrumentTradingSignals>(subscriberSettings)
                .SetMessageDeserializer(new GenericRabbitModelConverter<InstrumentTradingSignals>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new RabbitConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(x =>
                {
                    if (!exchanges.ContainsKey(x.Instrument.Exchange))
                    {
                        logger.LogWarning($"Received a trading signal for unconnected exchange {x.Instrument.Exchange}");
                        return Task.FromResult(0);
                    }
                    else
                    {
                        return exchanges[x.Instrument.Exchange].PlaceTradingOrders(x);    
                    }
                })
                .Start();  
        }

        public void Stop()
        {
            logger.LogInformation("Stop requested");
            ctSource.Cancel();

            foreach (var exchange in exchanges.Values)
            {
                exchange?.ClosePricesStream();    
            }
        }

        public class RabbitConsole : IConsole
        {
            public void WriteLine(string line)
            {
                Console.WriteLine(line);
            }
        }

        public IReadOnlyCollection<string> GetConnectedExchanges()
        {
            return exchanges.Keys.ToList();
        }

        public Exchange GetExchange(string name)
        {
            return exchanges[name];
        }

        
    }
}
