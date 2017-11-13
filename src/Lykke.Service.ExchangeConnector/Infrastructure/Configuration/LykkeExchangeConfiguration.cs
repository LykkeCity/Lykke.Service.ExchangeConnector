namespace TradingBot.Infrastructure.Configuration
{
    public class LykkeExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string[] Instruments { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }
        public double InitialRating { get; set; }

        public string ApiKey { get; set; }
        
        public string EndpointUrl { get; set; }

        public WampEndpointConfiguration WampEndpoint { get; set; }
    }

    public class WampEndpointConfiguration
    {
        public string Url { get; set; }
        public string PricesRealm { get; set; }
        public string PricesTopic { get; set; }
    }
}
