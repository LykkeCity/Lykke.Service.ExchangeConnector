using Microsoft.Extensions.Configuration;
using TradingBot.Common.Configuration;

namespace TradingBot.TheAlphaEngine.Configuration
{
    public class Configuration
    {
        public RabbitMqConfiguration RabbitMq { get; set; }
        
        public AlgorithmConfiguration Algorithm { get; set; }
        
        
        public static Configuration FromConfigurationRoot(IConfigurationRoot config)
        {
            return Instance = config.GetSection("TradingBot.Algorithm").Get<Configuration>();
        }
        
        public static Configuration Instance { get; protected set; }
    }
}