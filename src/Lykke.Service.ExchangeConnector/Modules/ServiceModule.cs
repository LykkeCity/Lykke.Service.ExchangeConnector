using Autofac;
using Autofac.Extras.DynamicProxy;
using TradingBot.Communications;
using TradingBot.Exchanges;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Bitfinex;
using TradingBot.Exchanges.Concrete.BitMEX;
using TradingBot.Exchanges.Concrete.GDAX;
using TradingBot.Exchanges.Concrete.HistoricalData;
using TradingBot.Exchanges.Concrete.Icm;
using TradingBot.Exchanges.Concrete.Kraken;
using TradingBot.Exchanges.Concrete.LykkeExchange;
using TradingBot.Exchanges.Concrete.StubImplementation;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Monitoring;

namespace TradingBot.Modules
{
    internal sealed class ServiceModule : Module
    {
        private readonly ExchangesConfiguration _config;

        public ServiceModule(ExchangesConfiguration config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
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

            builder.RegisterType<BitMexOrderBooksHarvester>()
                .WithParameter(new NamedParameter("exchangeName", BitMexExchange.Name))
                .SingleInstance();

            builder.RegisterType<BitfinexOrderBooksHarvester>()
                .WithParameter(new NamedParameter("exchangeName", BitfinexExchange.Name))
                .SingleInstance();

            builder.RegisterType<GdaxOrderBooksHarvester>()
                .WithParameter(new NamedParameter("exchangeName", GdaxExchange.Name))
                .SingleInstance();

            builder.RegisterInstance(_config.Icm)
                .AsSelf();
            builder.RegisterInstance(_config.Kraken)
                .AsSelf();
            builder.RegisterInstance(_config.Stub)
                .AsSelf();
            builder.RegisterInstance(_config.HistoricalData)
                .AsSelf();
            builder.RegisterInstance(_config.Lykke)
                .AsSelf();
            builder.RegisterInstance(_config.BitMex)
                .AsSelf();
            builder.RegisterInstance(_config.Bitfinex)
                .AsSelf();
            builder.RegisterInstance(_config.Gdax)
               .AsSelf();

            RegisterExchange<IcmExchange>(builder, _config.Icm.Enabled);
            RegisterExchange<KrakenExchange>(builder, _config.Kraken.Enabled);
            RegisterExchange<StubExchange>(builder, _config.Stub.Enabled);
            RegisterExchange<HistoricalDataExchange>(builder, _config.HistoricalData.Enabled);
            RegisterExchange<LykkeExchange>(builder, _config.Lykke.Enabled);
            RegisterExchange<BitMexExchange>(builder, _config.BitMex.Enabled);
            RegisterExchange<BitfinexExchange>(builder, _config.Bitfinex.Enabled);
            RegisterExchange<GdaxExchange>(builder, _config.Gdax.Enabled);
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
    }
}
