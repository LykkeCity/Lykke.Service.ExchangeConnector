namespace TradingBot.Infrastructure.Configuration
{
    public class OandaConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string[] Instruments { get; set; }
    }
}