namespace TradingBot.Infrastructure.Configuration
{
    public class LykkeExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string[] Instruments { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }
        
        public string ApiKey { get; set; }
        
        public string EndpointUrl { get; set; }
    }
}