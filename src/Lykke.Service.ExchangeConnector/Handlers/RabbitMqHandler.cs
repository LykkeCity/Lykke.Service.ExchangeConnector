using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Publisher;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    public class RabbitMqHandler<T> : Handler<T>
    {
        private readonly ILog _log;
        private readonly RabbitMqPublisher<T> _rabbitPublisher;

        public RabbitMqHandler(string connectionString, string exchangeName, bool durable = false, ILog log = null)
        {
            _log = log;
            var publisherSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName,
                IsDurable = durable
            };

            _rabbitPublisher = new RabbitMqPublisher<T>(publisherSettings)
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new GenericRabbitModelConverter<T>())
                .SetLogger(new LogToConsole())
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(publisherSettings))
                .SetConsole(new LogToConsole())
                .PublishSynchronously()
                .Start();
        }

        public override Task Handle(T message)
        {
            if (message is OrderBook ob)
            {
                _log?.WriteInfoAsync(GetType().Name, nameof(Handle), "Sending order books to the rabbit mq server", $"{ob.Timestamp.ToString()} asks count {ob.Asks.Count}").GetAwaiter().GetResult();
            }
            return _rabbitPublisher.ProduceAsync(message);
        }
    }
}
