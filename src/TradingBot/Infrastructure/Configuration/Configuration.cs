using Microsoft.Extensions.Configuration;
using TradingBot.Common.Configuration;

namespace TradingBot.Infrastructure.Configuration
{
    public class Configuration
    {
        public ExchangeConfiguration ExchangeConfig { get; set; }

        public RabbitMQConfiguration RabbitMQConfig { get; set; }

        public AzureTableConfiguration AzureTableConfig { get; set; }

        public static Configuration CreateDefaultConfig()
        {
            return new Configuration()
            {
                ExchangeConfig = new ExchangeConfiguration()
                {
                    Name = "kraken",
                    Instruments = new [] { "XXBTZUSD" }
                },
                RabbitMQConfig = new RabbitMQConfiguration()
                {
                    Enabled = true,
                    Host = "rabbit",
                    QueueName = $"kraken.XXBTZUSD"
                },
                AzureTableConfig = new AzureTableConfiguration()
                {
                    Enabled = false
                }
            };
        }

        public static Configuration FromConfigurationRoot(IConfigurationRoot config)
        {
            return new Configuration()
            {
                ExchangeConfig = config.GetSection("exchange").Get<ExchangeConfiguration>(),
                RabbitMQConfig = config.GetSection("rabbitMQ").Get<RabbitMQConfiguration>(),
                AzureTableConfig = config.GetSection("azureTable").Get<AzureTableConfiguration>()
            };
        }
    }
}
