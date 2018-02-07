using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.ExchangeDataStore.Core.Domain.Events;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Settings.ServiceSettings;
using Lykke.Service.ExchangeDataStore.Services.Helpers;
using System;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global - AsyncEvent must be public

namespace Lykke.Service.ExchangeDataStore.Services.DataHarvesters
{
    // ReSharper disable once ClassNeverInstantiated.Global - autofac instantiated
    public class OrderbookDataHarvester : IDisposable
    {
        private readonly ILog _log;
        private RabbitMqSubscriber<OrderBook> _rabbit;
        private readonly bool _enabled;
        private readonly RabbitMqExchangeConfiguration _orderBookQueueConfig;
        private string Component = nameof(OrderbookDataHarvester);

        public static AsyncEvent<OrderBook> OrderBookReceived = null;

        protected virtual Task OnOrderBookReceived(OrderBook orderBook)
        {
            return OrderBookReceived.NullableInvokeAsync(this, orderBook);
        }

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
                QueueName = _orderBookQueueConfig.Queue,
                IsDurable = true
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, rabbitSettings);
            _rabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy()) 
                .SetLogger(_log)
                .Subscribe(async orderBook =>
                {
                    _log.WriteInfo(Component, _orderBookQueueConfig.Queue, $"OrderBook received: {orderBook.Info()}");
                    await OnOrderBookReceived(orderBook);
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
            _rabbit?.Stop();
            _rabbit?.Dispose();
        }

        public void Stop()
        {
            if (!_enabled)
            {
                return;
            }
            _rabbit?.Stop();
            _log.WriteInfoAsync(Component, "Initializing", "", "Stopped");

        }
    }
}
