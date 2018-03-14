using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IcmExchangeConfiguration _config;
        private readonly IcmModelConverter _modelConverter;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<Trading.OrderBook> _orderBookHandler;
        private readonly ILog _log;
        private readonly RabbitMqSubscriber<OrderBook> _rabbit;
        private readonly bool _enabled;
        private readonly HashSet<string> _instruments;

        public IcmTickPriceHarvester(
            IcmExchangeConfiguration config, 
            IcmModelConverter modelConverter, 
            IHandler<TickPrice> tickPriceHandler,
            IHandler<Trading.OrderBook> orderBookHandler,
            ILog log)
        {
            _config = config;
            _modelConverter = modelConverter;
            _tickPriceHandler = tickPriceHandler;
            _orderBookHandler = orderBookHandler;
            _log = log;
            _enabled = config.RabbitMq.Enabled;
            if (!_enabled)
            {
                return;
            }
            _instruments = config.SupportedCurrencySymbols.Select(x => new Instrument(IcmExchange.Name, x.LykkeSymbol).Name).ToHashSet();
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
                .Subscribe(HandleOrderBook);
        }

        private async Task HandleOrderBook(OrderBook orderBook)
        {
            if (_instruments.Contains(orderBook.Asset) || _config.UseSupportedCurrencySymbolsAsFilter == false)
            {
                var tickPrice = _modelConverter.ToTickPrice(orderBook);
                if (tickPrice != null)
                {
                    await _tickPriceHandler.Handle(tickPrice);
                }

                await TrySendOrderBook(orderBook);
            }
        }

        private async Task TrySendOrderBook(OrderBook orderBook)
        {
            if ((orderBook.Asks == null || !orderBook.Asks.Any()) && (orderBook.Bids == null || !orderBook.Bids.Any()))
            {
                return;
            }

            var orderBookDto = new Trading.OrderBook(
                IcmExchange.Name,
                orderBook.Asset,
                orderBook.Asks?.Select(e => new VolumePrice(e.Price, e.Volume)).ToArray() ?? new VolumePrice[] { },
                orderBook.Bids?.Select(e => new VolumePrice(e.Price, e.Volume)).ToArray() ?? new VolumePrice[] { },
                orderBook.Timestamp);

            await _orderBookHandler.Handle(orderBookDto);
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
