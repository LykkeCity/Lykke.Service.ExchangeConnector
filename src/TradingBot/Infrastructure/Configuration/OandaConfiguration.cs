namespace TradingBot.Infrastructure.Configuration
{
    public class OandaConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public string[] Instruments { get; set; }
    }
}
