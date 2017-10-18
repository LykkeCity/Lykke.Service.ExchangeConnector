using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Exchanges;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Communications;
using TradingBot.Handlers;
using TradingBot.Trading;

namespace TradingBot
{
    internal sealed class ExchangeConnectorApplication : IApplicationFacade
    {
        private readonly ILog _log;
        private readonly Timer _timer;
        private readonly Dictionary<string, Exchange> _exchanges;
        private readonly AppSettings _config;
        private RabbitMqSubscriber<InstrumentTradingSignals> _signalSubscriber;


        public TranslatedSignalsRepository TranslatedSignalsRepository { get; }

        public ExchangeConnectorApplication(AppSettings config, TranslatedSignalsRepository translatedSignalsRepository, ExchangeFactory exchange, ILog log)
        {
            _config = config;
            _log = log;

            TranslatedSignalsRepository = translatedSignalsRepository;
            _exchanges = exchange.CreateExchanges()
                .ToDictionary(x => x.Name, x => x);
            _timer = new Timer(OnHeartbeat);
        }

        public async Task Start()
        {
            if (!_exchanges.Any())
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
                $"Price cycle starting for exchanges: {string.Join(", ", _exchanges.Keys)}...");

            if (_config.RabbitMq.Enabled)
            {
                SetupTradingSignalsSubscription(_config.RabbitMq); // can take too long
            }

            try
            {
                _exchanges.Values.ToList().ForEach(x => x.Start());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(ExchangeConnectorApplication), nameof(Start), "Starting exchange", ex);
                throw;
            }

            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
        }

        private async void OnHeartbeat(object state)
        {
            await _log.WriteInfoAsync(
                nameof(TradingBot),
                nameof(ExchangeConnectorApplication),
                nameof(Start),
                $"Exchange connector heartbeat: {DateTime.Now}. Exchanges statuses: {string.Join(", ", GetExchanges().Select(x => $"{x.Name}: {x.State}"))}");
        }

        private void SetupTradingSignalsSubscription(RabbitMqConfiguration rabbitConfig)
        {
            var handler = new TradingSignalsHandler(_exchanges, _log, TranslatedSignalsRepository);


            var subscriberSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = rabbitConfig.GetConnectionString(),
                ExchangeName = rabbitConfig.SignalsExchange,
                QueueName = rabbitConfig.SignalsQueue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, subscriberSettings);
            _signalSubscriber = new RabbitMqSubscriber<InstrumentTradingSignals>(subscriberSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<InstrumentTradingSignals>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(new LogToConsole())
                .Subscribe(handler.Handle)
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

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            foreach (var exchange in _exchanges.Values)
            {
                exchange?.Stop();
            }

        }

        public IReadOnlyCollection<IExchange> GetExchanges()
        {
            return _exchanges.Values;
        }

        public IExchange GetExchange(string name)
        {
            return _exchanges.ContainsKey(name) ? _exchanges[name] : throw new ArgumentException(@"Invalid exchangeName", nameof(name));
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _signalSubscriber?.Dispose();
        }
    }
}
