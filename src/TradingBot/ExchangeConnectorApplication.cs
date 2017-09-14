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
            
            if (config.RabbitMq.Enabled)
            {
                SetupTradingSignalsSubscription(config.RabbitMq); // can take too long
            }

            exchanges.Values.ToList().ForEach(x => x.Start());
            
            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
				logger.LogDebug($"Exchange connector heartbeat: {DateTime.Now}. Exchanges statuses: {string.Join(", ", GetExchanges().Select(x => $"{x.Name}: {x.State}"))}");
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
                        return exchanges[x.Instrument.Exchange].HandleTradingSignals(x);    
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
                exchange?.Stop();    
            }
        }

        public class RabbitConsole : IConsole
        {
            public void WriteLine(string line)
            {
                Console.WriteLine(line);
            }
        }

        public IReadOnlyCollection<Exchange> GetExchanges()
        {
            return exchanges.Values;
        }

        public Exchange GetExchange(string name)
        {
            return exchanges.ContainsKey(name) ? exchanges[name] : null;
        }
    }
}
