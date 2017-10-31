namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqExchangeConfiguration
    {
        public string Exchange { get; set; }
        
        public string Queue { get; set; }
    }
}
