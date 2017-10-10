using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using QuickFix;
using QuickFix.Transport;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Icm.Converters;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal class IcmExchange : Exchange
    {
        private readonly Common.Log.ILog _log;
        private readonly IcmConfig config;

        private RabbitMqSubscriber<OrderBook> rabbit;
        private SocketInitiator initiator;
        private IcmConnector connector;
        private readonly INoSQLTableStorage<FixMessageTableEntity> _tableStorage;
        public new static readonly string Name = "icm";

        public IcmExchange(
            IcmConfig config,
            TranslatedSignalsRepository translatedSignalsRepository,
            INoSQLTableStorage<FixMessageTableEntity> tableStorage,
            Common.Log.ILog log)
            : base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
            _tableStorage = tableStorage;
            _log = log;
        }

        protected override void StartImpl()
        {
            if (config.SocketConnection)
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "Socket connection is enabled");
                StartFixConnection();
            }
            else
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "Socket connection is desibled");
            }

            if (config.RabbitMq.Enabled && (config.PubQuotesToRabbit || config.SaveQuotesToAzure))
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "RabbitMQ connection is enabled");
                StartRabbitConnection();
            }
            else
            {
                _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartImpl), string.Empty,
                    "RabbitMQ connection is desibled");
            }
        }
        
        protected override void StopImpl()
        {
            rabbit?.Stop();
            initiator?.Stop();
            initiator?.Dispose();
        }
        
        private void StartFixConnection()
        {
            var settings = new SessionSettings(config.GetFixConfigAsReader());

            _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartFixConnection), string.Join("/n", config.FixConfiguration), "Starting fix connection with configuration").Wait();
            
            var repository = new AzureFixMessagesRepository(_tableStorage);
            
            connector = new IcmConnector(config, repository, LykkeLog);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new LykkeLogFactory(_log);

            connector.OnTradeExecuted += CallExecutedTradeHandlers;
            connector.Connected += OnConnected;
            connector.Disconnected += OnStopped;
            
            initiator = new SocketInitiator(connector, storeFactory, settings, logFactory);
            
            _log.WriteInfoAsync(nameof(IcmExchange), nameof(StartFixConnection), string.Empty, "SocketInitiator is about to start").Wait();
            initiator.Start();
        }

        /// <summary>
        /// For ICM we use internal RabbitMQ exchange with pricefeed
        /// </summary>
        private void StartRabbitConnection()
        {
            var rabbitSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = config.RabbitMq.GetConnectionString(),
                ExchangeName = config.RabbitMq.RatesExchange
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, rabbitSettings);
            rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new ExchangeConnectorApplication.RabbitConsole())
                .SetLogger(_log)
                .Subscribe(async orderBook =>
                    {
                        if (Instruments.Any(x => x.Name == orderBook.Asset))
                            await CallTickPricesHandlers(orderBook.ToInstrumentTickPrices());
                    })
                .Start();
        }
        
        protected override async Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            await LykkeLog.WriteInfoAsync(
                nameof(Icm),
                nameof(IcmExchange),
                nameof(AddOrderImpl),
                $"About to place new order for instrument {instrument}: {signal}");
            
            return connector.AddOrder(instrument, signal, translatedSignal);
        }

        protected override async Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            await LykkeLog.WriteInfoAsync(
                nameof(Icm),
                nameof(IcmExchange),
                nameof(AddOrderImpl),
                $"Canceling order {signal}");

            return connector.CancelOrder(instrument, signal, translatedSignal);
        }

        public override Task<ExecutedTrade> GetOrder(string orderId, Instrument instrument)
        {
            return connector.GetOrderInfoAndWaitResponse(instrument, orderId);
        }

        public override Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            return connector.GetAllOrdersInfo(timeout);
        }

        public override Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            return connector.AddOrderAndWaitResponse(instrument, signal, translatedSignal, timeout);
        }

        public override Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            return connector.CancelOrderAndWaitResponse(instrument, signal, translatedSignal, timeout);
        }
    }
}
