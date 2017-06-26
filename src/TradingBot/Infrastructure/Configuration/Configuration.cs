using Microsoft.Extensions.Configuration;
using TradingBot.Common.Configuration;

namespace TradingBot.Infrastructure.Configuration
{
    public class Configuration
    {
        public AspNetConfiguration AspNet { get; set; }
        
        public ExchangesConfiguration Exchanges { get; set; }

        public RabbitMqConfiguration RabbitMq { get; set; }

        public AzureTableConfiguration AzureTable { get; set; }

        public LoggerConfiguration Logger { get; set; }
        
        
        public static Configuration FromConfigurationRoot(IConfigurationRoot config)
        {
            return Instance = config.GetSection("TradingBot").Get<Configuration>();
        }
        
        public static Configuration Instance { get; protected set; }
    }
}
