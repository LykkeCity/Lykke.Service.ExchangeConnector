namespace TradingBot.Infrastructure.Configuration
{
    public class RabbitMqMultyExchangeConfiguration : RabbitMqConfigurationBase
    {
        public string RatesExchange { get; set; }
        
        public string SignalsExchange { get; set; }
        
        public string TradesExchange { get; set; }
        
        public string SignalsQueue { get; set; }
        
        public string AcknowledgementsExchange { get; set; }
        
        public string AcknowledgementsQueue { get; set; }
    }
}
