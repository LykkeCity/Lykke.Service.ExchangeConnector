using Lykke.SettingsReader.Attributes;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class RabbitMqExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        [Optional]
        public string Exchange { get; set; }
        [Optional]
        public string Queue { get; set; }
        
        [AmqpCheck]
        public string ConnectionString { get; set; }
    }
}
