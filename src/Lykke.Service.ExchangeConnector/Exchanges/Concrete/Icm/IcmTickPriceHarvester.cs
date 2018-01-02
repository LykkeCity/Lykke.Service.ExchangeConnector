using System.Linq;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.Icm.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal sealed class IcmTickPriceHarvester : IStartable, IStopable
    {
        private readonly ILog _log;
        private readonly RabbitMqSubscriber<OrderBook> _rabbit;
        private readonly bool _enabled;

        public IcmTickPriceHarvester(IcmConfig config, IcmModelConverter modelConverter, IHandler<TickPrice> tickPriceHandler, ILog log)
        {
            _log = log;
            _enabled = config.RabbitMq.Enabled;
            if (!_enabled)
            {
                return;
            }
            var instruments = config.SupportedCurrencySymbols.Select(x => new Instrument(IcmExchange.Name, x.LykkeSymbol)).ToList();
            var rabbitSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = config.RabbitMq.ConnectionString,
                ExchangeName = config.RabbitMq.Exchange,
                QueueName = config.RabbitMq.Queue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, rabbitSettings);
            _rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(_log)
                .Subscribe(async orderBook =>
                {
                    if (instruments.Any(x => x.Name == orderBook.Asset))
                    {
                        var tickPrice = modelConverter.ToTickPrice(orderBook);
                        if (tickPrice != null)
                        {
                            await tickPriceHandler.Handle(tickPrice);
                        }
                    }
                });
        }

        public void Start()
        {
            if (!_enabled)
            {
                return;
            }
            _rabbit.Start();
            _log.WriteInfoAsync(nameof(IcmTickPriceHarvester), "Initializing", "", "Started");
        }

        public void Dispose()
        {
            _rabbit?.Dispose();
        }

        public void Stop()
        {
            if (!_enabled)
            {
                return;
            }
            _rabbit.Stop();
            _log.WriteInfoAsync(nameof(IcmTickPriceHarvester), "Initializing", "", "Stopped");

        }
    }
}
