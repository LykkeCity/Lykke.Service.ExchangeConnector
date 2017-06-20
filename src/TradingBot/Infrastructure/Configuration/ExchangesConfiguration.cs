namespace TradingBot.Infrastructure.Configuration
{
    public class ExchangesConfiguration
    {
        public IcmConfig Icm { get; set; }
        
        public KrakenConfig Kraken { get; set; }
    }
}
