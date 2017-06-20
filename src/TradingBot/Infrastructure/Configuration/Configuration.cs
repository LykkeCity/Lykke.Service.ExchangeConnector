using Microsoft.Extensions.Configuration;
using TradingBot.Common.Configuration;

namespace TradingBot.Infrastructure.Configuration
{
    public class Configuration
    {
        public ExchangesConfiguration Exchanges { get; set; }

        public RabbitMqConfiguration RabbitMq { get; set; }

        public AzureTableConfiguration AzureTable { get; set; }

        public LoggerConfiguration Logger { get; set; }

        
        public static Configuration FromConfigurationRoot(IConfigurationRoot config)
        {
            return config.GetSection("TradingBot").Get<Configuration>();
        }
    }
}
