using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using TradingBot.Trading;

namespace TradingBot.Handlers
{
    public class RabbitMqHandler<T> : Handler<T>
    {
        private readonly RabbitMqPublisher<T> rabbitPublisher;
        
        public RabbitMqHandler(string connectionString, string exchangeName)
        {
            var publisherSettings = new RabbitMqPublisherSettings()
            {
                ConnectionString = connectionString,
                ExchangeName = exchangeName
            };
            
            rabbitPublisher = new RabbitMqPublisher<T>(publisherSettings)
                .SetSerializer(new GenericRabbitModelConverter<T>())
                .SetLogger(new LogToConsole())
                .SetPublishStrategy(new DefaultFnoutPublishStrategy())
                .SetConsole(new ExchangeConnectorApplication.RabbitConsole())
                .Start();
        }
        
        public override Task Handle(T message)
        {
            return rabbitPublisher.ProduceAsync(message);
        }
    }
}