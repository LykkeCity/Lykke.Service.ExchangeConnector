namespace TradingBot.Infrastructure.Configuration
{
    public class KrakenConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public string ApiKey { get; set; }
        
        public string PrivateKey { get; set; }
        
        public string[] Instruments { get; set; }
    }
}
