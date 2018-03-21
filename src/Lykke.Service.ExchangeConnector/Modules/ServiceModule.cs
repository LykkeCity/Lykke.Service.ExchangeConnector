using Autofac;
using Autofac.Extras.DynamicProxy;
using Common;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Communications;
using TradingBot.Exchanges;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Exchanges.Concrete.BitMEX;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Exchanges.Concrete.HistoricalData;
using TradingBot.Exchanges.Concrete.Icm;
using TradingBot.Exchanges.Concrete.Jfd;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Exchanges.Concrete.LykkeExchange;
using TradingBot.Exchanges.Concrete.StubImplementation;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Monitoring;
using TradingBot.Trading;
using TradingOrderBook = TradingBot.Trading.OrderBook;
using LykkeOrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;

namespace TradingBot.Modules
{
    internal sealed class ServiceModule : Module
    {
        private readonly AppSettings _config;
        private readonly ILog _log;

        public ServiceModule(AppSettings config, ILog log)
        {
            _config = config;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {

            builder.RegisterGeneric(typeof(RabbitMqHandler<>));

            builder.RegisterType<InverseDateTimeRowKeyProvider>();

            builder.RegisterType<TranslatedSignalsRepository>();

            builder.RegisterType<OrderBookEventsRepository>();

            builder.RegisterType<OrderBookSnapshotsRepository>();

            builder.RegisterType<ExchangeFactory>();

            builder.RegisterType<ExchangeCallsInterceptor>()
                .SingleInstance();

            builder.RegisterType<ExchangeStatisticsCollector>()
                .SingleInstance();

            builder.RegisterType<ExchangeRatingValuer>()
                .As<IExchangeRatingValuer>()
                .SingleInstance();

            builder.RegisterType<ExchangeConnectorApplication>()
                .As<IApplicationFacade>()
                .SingleInstance();

            builder.RegisterType<BitmexSocketSubscriberDecorator>()
                .As<IBitmexSocketSubscriber>()
                .SingleInstance();

            builder.RegisterType<BitMexOrderHarvester>()
                .SingleInstance();

            builder.RegisterType<BitMexPriceHarvester>()
                .SingleInstance();

            builder.RegisterType<BitMexOrderBooksHarvester>()
                .SingleInstance();

            builder.RegisterType<BitMexExecutionHarvester>()
                .SingleInstance();

            builder.RegisterType<BitfinexOrderBooksHarvester>()
                .SingleInstance();

            builder.RegisterType<BitfinexExecutionHarvester>()
                .SingleInstance();

            builder.RegisterType<BitfinexWebSocketSubscriber>()
                .WithParameter("authenticate", true)
                .As<IBitfinexWebSocketSubscriber>()
                .SingleInstance();

            builder.RegisterType<GdaxOrderBooksHarvester>()
                .SingleInstance();

            builder.RegisterType<JfdOrderBooksHarvester>()
                .SingleInstance();

            builder.RegisterType<JfdModelConverter>()
                .SingleInstance();         
            
            builder.RegisterType<AzureFixMessagesRepository>()
                .As<IAzureFixMessagesRepository>()
                .SingleInstance();    
            
            builder.RegisterType<IcmTickPriceHarvester>()
                .As<IStartable>()
                .As<IStopable>()
                .AsSelf()
                .SingleInstance();  
            
            builder.RegisterType<IcmTradeSessionConnector>()
                .SingleInstance();   
            
            builder.RegisterType<BitfinexModelConverter>()
                .SingleInstance(); 
            
            builder.RegisterType<IcmModelConverter>()
                .SingleInstance();

            builder.RegisterType<TradingSignalsHandler>()
                .WithParameter("apiTimeout", _config.AspNet.ApiTimeout)
                .WithParameter("enabled", _config.RabbitMq.Signals.Enabled)
                .As<IStartable>()
                .AsSelf()
                .SingleInstance();

            foreach (var cfg in _config.Exchanges)
            {
                builder.RegisterInstance(cfg)
                    .As(cfg.GetType());
            }

            RegisterExchange<IcmExchange>(builder, _config.Exchanges.Icm.Enabled);
            RegisterExchange<KrakenExchange>(builder, _config.Exchanges.Kraken.Enabled);
            RegisterExchange<StubExchange>(builder, _config.Exchanges.Stub.Enabled);
            RegisterExchange<HistoricalDataExchange>(builder, _config.Exchanges.HistoricalData.Enabled);
            RegisterExchange<LykkeExchange>(builder, _config.Exchanges.Lykke.Enabled);
            RegisterExchange<BitMexExchange>(builder, _config.Exchanges.BitMex.Enabled);
            RegisterExchange<BitfinexExchange>(builder, _config.Exchanges.Bitfinex.Enabled);
            RegisterExchange<GdaxExchange>(builder, _config.Exchanges.Gdax.Enabled);
            RegisterExchange<JfdExchange>(builder, _config.Exchanges.Jfd.Enabled);

            RegisterTradeSignalSubscriber(builder);

            RegisterRabbitMqHandler<TickPrice>(builder, _config.RabbitMq.TickPrices, "tickHandler");
            RegisterRabbitMqHandler<ExecutionReport>(builder, _config.RabbitMq.Trades);
            RegisterRabbitMqHandler<TradingOrderBook>(builder, _config.RabbitMq.OrderBooks, "orderBookHandler");

            builder.RegisterType<TickPriceHandlerDecorator>()
                .WithParameter((info, context) => info.Name == "rabbitMqHandler",
                               (info, context) => context.ResolveNamed<IHandler<TickPrice>>("tickHandler"))
                .SingleInstance()
                .As<IHandler<TickPrice>>();

            builder.RegisterType<OrderBookHandlerDecorator>()
                .WithParameter((info, context) => info.Name == "rabbitMqHandler",
                    (info, context) => context.ResolveNamed<IHandler<TradingOrderBook>>("orderBookHandler"))
                .SingleInstance()
                .As<IHandler<LykkeOrderBook>>();
        }

        private static void RegisterExchange<T>(ContainerBuilder container, bool enabled)
        {

            if (enabled)
            {
                container.RegisterType<T>()
                    .As<Exchange>()
                    .SingleInstance()
                    .EnableClassInterceptors()
                    .InterceptedBy(typeof(ExchangeCallsInterceptor));
            }

        }

        private static void RegisterRabbitMqHandler<T>(ContainerBuilder container, RabbitMqExchangeConfiguration exchangeConfiguration, string regKey = "")
        {
            container.RegisterType<RabbitMqHandler<T>>()
                .WithParameter("connectionString", exchangeConfiguration.ConnectionString)
                .WithParameter("exchangeName", exchangeConfiguration.Exchange)
                .WithParameter("enabled", exchangeConfiguration.Enabled)
                .Named<IHandler<T>>(regKey)
                .As<IHandler<T>>();
        }

        private void RegisterTradeSignalSubscriber(ContainerBuilder container)
        {
            var subscriberSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _config.RabbitMq.Signals.ConnectionString,
                ExchangeName = _config.RabbitMq.Signals.Exchange,
                QueueName = _config.RabbitMq.Signals.Queue,
                IsDurable = false
            };

            var errorStrategy = new DefaultErrorHandlingStrategy(_log, subscriberSettings);
            var subscriber = new RabbitMqSubscriber<TradingSignal>(subscriberSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<TradingSignal>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(_log);

            container.Register(c => subscriber);
        }
    }
}
