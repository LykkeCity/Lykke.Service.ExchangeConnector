using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Publisher;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    public class RabbitMqHandler<T> : Handler<T>, IDisposable
    {
        private readonly RabbitMqPublisher<T> _rabbitPublisher;
        private readonly object _sync = new object();

        public RabbitMqHandler(string connectionString, string exchangeName, bool durable = false, ILog log = null)
        {
            var publisherSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName,
                IsDurable = durable
            };

            _rabbitPublisher = new RabbitMqPublisher<T>(publisherSettings)
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new GenericRabbitModelConverter<T>())
                .SetLogger(log ?? new LogToConsole())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(publisherSettings))
                .SetConsole(new LogToConsole())
                .PublishSynchronously()
                .Start();
        }

        public override Task Handle(T message)
        {
            lock (_sync)
            { 
                return _rabbitPublisher.ProduceAsync(message);
            }
        }

        public void Dispose()
        {
            _rabbitPublisher.Stop();
            _rabbitPublisher.Dispose();
        }
    }
}
