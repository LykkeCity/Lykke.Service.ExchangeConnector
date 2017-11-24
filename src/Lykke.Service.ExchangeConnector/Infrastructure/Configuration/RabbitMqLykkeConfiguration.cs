namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqLykkeConfiguration : RabbitMqConfigurationBase
    {
        public RabbitMqExchangeConfiguration OrderBook { get; set; }
        
        public RabbitMqExchangeConfiguration Orders { get; set; }
    }
}
