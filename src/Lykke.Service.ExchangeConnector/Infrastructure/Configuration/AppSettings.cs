using System.Collections.Generic;
using Lykke.AzureQueueIntegration;

namespace TradingBot.Infrastructure.Configuration
{
    public class AppSettings
    {
        public AspNetConfiguration AspNet { get; set; }

        public ExchangesConfiguration Exchanges { get; set; }

        public RabbitMqMultyExchangeConfiguration RabbitMq { get; set; }

        public IReadOnlyDictionary<string, bool> SaveQuotesToAzure { get; set; }

        public AzureTableConfiguration AzureStorage { get; set; }
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
}
