namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqMultyExchangeConfiguration
    {   
        public RabbitMqExchangeConfiguration TickPrices { get; set; }
        public RabbitMqExchangeConfiguration Signals { get; set; }
        public RabbitMqExchangeConfiguration Trades { get; set; }
        public RabbitMqExchangeConfiguration OrderBooks { get; set; }
    }
}
