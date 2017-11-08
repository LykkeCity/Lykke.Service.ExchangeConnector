using Lykke.AzureQueueIntegration;

namespace TradingBot.Infrastructure.Configuration
{
    public class AppSettings
    {
        public AspNetConfiguration AspNet { get; set; }

        public ExchangesConfiguration Exchanges { get; set; }

        public RabbitMqMultyExchangeConfiguration RabbitMq { get; set; }

        public OrderBookRabbitMqConfiguration OrderBooksRabbitMq { get; set; }

        public AzureTableConfiguration AzureStorage { get; set; }

        public WampEndpointConfiguration WampEndpoint { get; set; }
    }

    public class TradingBotSettings
    {
        public AppSettings TradingBot { get; set; }

        public SlackNotificationSettings SlackNotifications { get; set; }
    }

    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }

    public class WampEndpointConfiguration
    {
        public string Url { get; set; }
        public string PricesRealm { get; set; }
        public string PricesTopic { get; set; }
    }
}
