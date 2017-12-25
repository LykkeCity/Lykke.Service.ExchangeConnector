namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqLykkeConfiguration
    {
        public RabbitMqExchangeConfiguration OrderBook { get; set; }
        
        public RabbitMqExchangeConfiguration Orders { get; set; }
    }
}
