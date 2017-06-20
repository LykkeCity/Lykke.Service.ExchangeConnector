namespace TradingBot.Infrastructure.Configuration
{
    public class KrakenConfig
    {
        public bool Enabled { get; set; }
        
        public string[] Instruments { get; set; }
    }
}