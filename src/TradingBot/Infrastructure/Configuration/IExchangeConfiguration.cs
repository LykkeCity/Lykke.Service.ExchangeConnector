namespace TradingBot.Infrastructure.Configuration
{
    public interface IExchangeConfiguration
    {
        bool Enabled { get; set; }
        
        string[] Instruments { get; set; }
        
        bool SaveQuotesToAzure { get; set; }
        
        bool PubQuotesToRabbit { get; set; }
    }
}