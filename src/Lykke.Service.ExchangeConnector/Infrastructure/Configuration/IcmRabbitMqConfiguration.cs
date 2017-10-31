namespace TradingBot.Infrastructure.Configuration
{
    public class IcmRabbitMqConfiguration : RabbitMqConfigurationBase
    {
        public string Exchange { get; set; }
    }
}
