namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqMultyExchangeConfiguration : RabbitMqConfigurationBase
    {   
        public RabbitMqExchangeConfiguration TickPrices { get; set; }
        public RabbitMqExchangeConfiguration Signals { get; set; }
        public RabbitMqExchangeConfiguration Trades { get; set; }
        public RabbitMqExchangeConfiguration Acknowledgements { get; set; }
    }
}
