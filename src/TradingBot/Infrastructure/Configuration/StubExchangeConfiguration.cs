namespace TradingBot.Infrastructure.Configuration
{
    public class StubExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }
        
        public int PricesIntervalInMilliseconds { get; set; }
        
        public int PricesPerInterval { get; set; }
        
        public string[] Instruments { get; set; }
    }
}