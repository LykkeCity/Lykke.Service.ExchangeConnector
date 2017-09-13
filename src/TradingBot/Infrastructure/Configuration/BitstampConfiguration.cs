namespace TradingBot.Infrastructure.Configuration
{
    public class BitstampConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        public string[] Instruments { get; set; }
        public bool SaveQuotesToAzure { get; set; }
        public bool PubQuotesToRabbit { get; set; }
        
        public string ApplicationKey { get; set; }
    }
}