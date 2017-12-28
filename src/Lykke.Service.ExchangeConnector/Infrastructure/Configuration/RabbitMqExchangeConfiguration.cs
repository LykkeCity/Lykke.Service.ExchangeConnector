using Lykke.SettingsReader.Attributes;

namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        [Optional]
        public string Exchange { get; set; }
        
        [Optional]
        public string Queue { get; set; }
        
        public string ConnectionString { get; set; }
    }
}
