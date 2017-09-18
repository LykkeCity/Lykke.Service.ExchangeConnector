using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Communications;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot
{
    public class ExchangeConnectorApplication : IApplicationFacade
    {
        private ILog _log;

        public TranslatedSignalsRepository TranslatedSignalsRepository { get; }

        public ExchangeConnectorApplication(
            AppSettings config,
            IReloadingManager<TradingBotSettings> settingsManager,
            INoSQLTableStorage<FixMessageTableEntity> fixMessagesStorage,
            ILog log)
        {
            this.config = config;
            _log = log;
            var signalsStorage = AzureTableStorage<TranslatedSignalTableEntity>.Create(
                settingsManager.ConnectionString(i => i.TradingBot.AzureStorage.StorageConnectionString), "translatedSignals", new LogToConsole());
            TranslatedSignalsRepository = new TranslatedSignalsRepository(signalsStorage, new InverseDateTimeRowKeyProvider());

            exchanges = ExchangeFactory.CreateExchanges(config, TranslatedSignalsRepository, settingsManager, fixMessagesStorage, log)
                .ToDictionary(x => x.Name, x => x);
        }

        private readonly Dictionary<string, Exchange> exchanges;
        
		private CancellationTokenSource ctSource;
        private readonly AppSettings config;
        private RabbitMqSubscriber<InstrumentTradingSignals> signalSubscriber;

        
        public async Task Start()
        {
            ctSource = new CancellationTokenSource();
            var token = ctSource.Token;

            if (!exchanges.Any())
            {
                await _log.WriteInfoAsync(
                    nameof(TradingBot),
                    nameof(ExchangeConnectorApplication),
                    nameof(Start),
                    "There is no enabled exchange.");
                return;
            }

            await _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Start),
                $"Price cycle starting for exchanges: {string.Join(", ", exchanges.Keys)}...");
            
            if (config.RabbitMq.Enabled)
            {
                SetupTradingSignalsSubscription(config.RabbitMq); // can take too long
            }

            exchanges.Values.ToList().ForEach(x => x.Start());
            
            while (!token.IsCancellationRequested)
			{
                await Task.Delay(TimeSpan.FromSeconds(15), token);
                await _log.WriteInfoAsync(
                    nameof(TradingBot),
                    nameof(ExchangeConnectorApplication),
                    nameof(Start),
                    $"Exchange connector heartbeat: {DateTime.Now}. Exchanges statuses: {string.Join(", ", GetExchanges().Select(x => $"{x.Name}: {x.State}"))}");
			}
        }

        private void SetupTradingSignalsSubscription(RabbitMqConfiguration rabbitConfig)
        {
            var subscriberSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = rabbitConfig.GetConnectionString(),
                ExchangeName = rabbitConfig.SignalsExchange,
                QueueName = rabbitConfig.SignalsQueue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, subscriberSettings);
            signalSubscriber = new RabbitMqSubscriber<InstrumentTradingSignals>(subscriberSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<InstrumentTradingSignals>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new RabbitConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(x =>
                {
                    if (!exchanges.ContainsKey(x.Instrument.Exchange))
                    {
                        _log.WriteWarningAsync(
                            nameof(TradingBot),
                            nameof(ExchangeConnectorApplication),
                            nameof(SetupTradingSignalsSubscription),
                            $"Received a trading signal for unconnected exchange {x.Instrument.Exchange}")
                            .Wait();
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
            _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Stop),
                "Stop requested")
                .Wait();
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
