using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Publisher;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    internal class RabbitMqHandler<T> : IHandler<T>, IDisposable
    {
        private readonly bool _enabled;
        private readonly RabbitMqPublisher<T> _rabbitPublisher;
        private readonly object _sync = new object();

        public RabbitMqHandler(string connectionString, string exchangeName, bool enabled, ILog log, bool durable = true)
        {
            _enabled = enabled;
            if (!enabled)
            {
                log.WriteInfoAsync($"{GetType()}", "Constructor", $"A rabbit mq handler for {typeof(T)} is disabled");
                return;
            }
            var publisherSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName,
                IsDurable = durable
            };

            _rabbitPublisher = new RabbitMqPublisher<T>(publisherSettings)
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new GenericRabbitModelConverter<T>())
                .SetLogger(log)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(publisherSettings))
                .PublishSynchronously()
                .Start();
        }

        public Task Handle(T message)
        {
            if (!_enabled)
            {
                return Task.CompletedTask;
            }
            lock (_sync)
            {
                return _rabbitPublisher.ProduceAsync(message);
            }
        }

        public void Dispose()
        {
            _rabbitPublisher?.Stop();
            _rabbitPublisher?.Dispose();
        }
    }
}
