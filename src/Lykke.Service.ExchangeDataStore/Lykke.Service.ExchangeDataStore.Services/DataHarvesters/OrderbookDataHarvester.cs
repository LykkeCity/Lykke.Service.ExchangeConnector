using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Services.Helpers;

namespace Lykke.Service.ExchangeDataStore.Services.DataHarvesters
{
    public class OrderbookDataHarvester : IStartable, IStopable
    {
        private readonly ILog _log;
        private RabbitMqSubscriber<OrderBook> _rabbit;
        private readonly bool _enabled;
        private readonly RabbitMqExchangeConfiguration _orderBookQueueConfig;
        private string Component = nameof(OrderbookDataHarvester);

        public OrderbookDataHarvester(ILog log, RabbitMqExchangeConfiguration orderBookQueueConfig)
        {
            _log = log;
            _enabled = orderBookQueueConfig.Enabled;
            _orderBookQueueConfig = orderBookQueueConfig;
            SubsribeToMessageQueue();
        }

        private void SubsribeToMessageQueue()
        {
            var rabbitSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _orderBookQueueConfig.ConnectionString,
                ExchangeName = _orderBookQueueConfig.Exchange,
                QueueName = _orderBookQueueConfig.Queue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, rabbitSettings);
            _rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(_log)
                .Subscribe(async orderBook =>
                {
                    _log.WriteInfo(Component, "", $"OrderBook recived: {orderBook.Info()}");                    
                });
        }

        public void Start()
        {
            if (!_enabled)
            {
                return;
            }
            _rabbit.Start();
            _log.WriteInfoAsync(Component, "Initializing", "", "Started");
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
            _log.WriteInfoAsync(Component, "Initializing", "", "Stopped");

        }
    }
}
