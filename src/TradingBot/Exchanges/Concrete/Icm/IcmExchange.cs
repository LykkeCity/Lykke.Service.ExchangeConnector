using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Logging;
using Polly;
using QuickFix;
using QuickFix.Transport;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Icm.Converters;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm
{
    public class IcmExchange : Exchange
    {
        public new static readonly string Name = "icm";
        
        public IcmExchange(IcmConfig config, TranslatedSignalsRepository translatedSignalsRepository) : base(Name, config, translatedSignalsRepository)
        {
            this.config = config;
        }

        private readonly IcmConfig config;

        private RabbitMqSubscriber<OrderBook> rabbit;
        private SocketInitiator initiator;
        private IcmConnector connector;


        /// <summary>
        /// For ICM we use internal RabbitMQ exchange with pricefeed
        /// </summary>
        public override Task OpenPricesStream()
        {
            StartRabbitConnection();

            return Task.FromResult(0);
        }
        
        public override Task ClosePricesStream()
        {
            rabbit?.Stop();
            initiator?.Stop();
            initiator?.Dispose();

            return Task.FromResult(0);
        }

        private void StartRabbitConnection()
        {
            var rabbitSettings = new RabbitMqSubscriberSettings()
            {
                ConnectionString = config.RabbitMq.GetConnectionString(),
                ExchangeName = config.RabbitMq.RatesExchange
            };

            rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new ExchangeConnectorApplication.RabbitConsole())
                .SetLogger(new LykkeLogToAzureStorage("IcmPriceSubscriber", 
                        new AzureTableStorage<LogEntity>(Configuration.Instance.AzureStorage.StorageConnectionString,
                        Configuration.Instance.LogsTableName,
                        new LogToConsole())))
                .Subscribe(async orderBook =>
                    {
                        if (Instruments.Any(x => x.Name == orderBook.Asset))
                            await CallHandlers(orderBook.ToInstrumentTickPrices());
                    })
                .Start();
        }
        
        private void StartFixConnection()
        {
            var settings = new SessionSettings(config.GetFixConfigAsReader());

            var repository = new AzureFixMessagesRepository(Configuration.Instance.AzureStorage.StorageConnectionString, "fixMessages");
            
            connector = new IcmConnector(config, repository);
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new ScreenLogFactory(settings);

            connector.OnTradeExecuted += CallExecutedTradeHandlers;
            
            initiator = new SocketInitiator(connector, storeFactory, settings, logFactory);
            initiator.Start();
            
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            StartFixConnection();
            
            var retry = Policy
                .HandleResult<bool>(x => !x)
                .WaitAndRetry(5, attempt => TimeSpan.FromSeconds(10));
            
            return Task.FromResult(retry.Execute(() => initiator.IsLoggedOn));
        }
        
        protected override Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            Logger.LogInformation($"About to place new order for instrument {instrument}: {signal}");
            return Task.FromResult(connector.AddOrder(instrument, signal, translatedSignal));
        }

        protected override Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            Logger.LogInformation($"Cancelling order {signal}");

            return Task.FromResult(connector.CancelOrder(instrument, signal, translatedSignal));
        }

        public Task<ExecutedTrade> GetOrderInfo(Instrument instrument, string orderId)
        {
            return connector.GetOrderInfoAndWaitResponse(instrument, orderId);
        }

        public Task<IEnumerable<ExecutedTrade>> GetAllOrdersInfo(TimeSpan timeout)
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
