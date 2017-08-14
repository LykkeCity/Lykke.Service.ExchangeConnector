namespace TradingBot.Infrastructure.Configuration
{
    public class StubExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public int PricesIntervalInMilliseconds { get; set; }
        
        public int PricesPerInterval { get; set; }
        
        public string[] Instruments { get; set; }
    }
}