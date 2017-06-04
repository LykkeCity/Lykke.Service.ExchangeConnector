using Microsoft.Extensions.Configuration;

namespace TradingBot.Infrastructure.Configuration
{
    public class Configuration
    {
        public ExchangeConfiguration ExchangeConfig { get; set; }

        public RabbitMQConfiguration RabbitMQConfig { get; set; }

        public static Configuration CreateDefaultConfig()
        {
            return new Configuration()
            {
                ExchangeConfig = new ExchangeConfiguration()
                {
                    Name = "kraken",
                    Instrument = "XXBTZUSD"
                },
                RabbitMQConfig = new RabbitMQConfiguration()
                {
                    Host = "rabbit",
                    QueueName = $"kraken.XXBTZUSD"
                }
            };
        }

        public static Configuration FromConfigurationRoot(IConfigurationRoot config)
        {
            return new Configuration()
            {
                ExchangeConfig = config.GetSection("exchange").Get<ExchangeConfiguration>(),
                RabbitMQConfig = config.GetSection("rabbitMQ").Get<RabbitMQConfiguration>()
            };
        }
    }
}
