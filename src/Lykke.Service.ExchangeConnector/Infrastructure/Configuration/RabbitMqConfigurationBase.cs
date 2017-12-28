namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqConfigurationBase
    {
        public bool Enabled { get; set; }
        
        public string ConnectionString { get; set; }
    }
}
