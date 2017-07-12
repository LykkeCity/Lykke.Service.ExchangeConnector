using System.Threading.Tasks;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using TradingBot.Common.Configuration;
using TradingBot.Common.Trading;

namespace TradingBot
{
    public abstract class ExecutedOrdersHandler
    {
        public abstract Task Handle(ExecutedTrade trade);
    }

    public class ExecutedOrdersRabbitPublisher : ExecutedOrdersHandler
    {
        private readonly RabbitMqPublisher<ExecutedTrade> rabbitPublisher;

        public ExecutedOrdersRabbitPublisher(RabbitMqConfiguration rabbitConfig)
        {
            var publisherSettings = new RabbitMqPublisherSettings()
            {
                ConnectionString = rabbitConfig.GetConnectionString(),
                ExchangeName = rabbitConfig.TradesExchange
            };

            rabbitPublisher = new RabbitMqPublisher<ExecutedTrade>(publisherSettings)
                .SetSerializer(new GenericRabbitModelConverter<ExecutedTrade>())
                .SetLogger(new LogToConsole())
                .SetPublishStrategy(new DefaultFnoutPublishStrategy())
                .SetConsole(new GetPricesCycle.RabbitConsole())
                .Start();
        }

        public override Task Handle(ExecutedTrade executedTrade)
        {
            return rabbitPublisher.ProduceAsync(executedTrade);
        }
    }
}