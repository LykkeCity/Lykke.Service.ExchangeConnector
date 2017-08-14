namespace TradingBot.Infrastructure.Configuration
{
    public class KrakenConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string[] Instruments { get; set; }
    }
}