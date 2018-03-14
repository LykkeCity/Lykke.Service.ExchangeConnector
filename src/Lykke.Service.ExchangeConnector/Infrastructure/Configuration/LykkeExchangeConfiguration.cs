using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class LykkeExchangeConfiguration : IExchangeConfiguration
    {
        public LykkeExchangeConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }
        public double InitialRating { get; set; }

        [Lykke.SettingsReader.Attributes.Optional]
        public bool? UseSupportedCurrencySymbolsAsFilter { get; set; }

        public string ApiKey { get; set; }
        
        public string EndpointUrl { get; set; }
        
        public string ClientId { get; set; }

        public WampEndpointConfiguration WampEndpoint { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
        
        public RabbitMqLykkeConfiguration RabbitMq { get; set; }
    }

    public class WampEndpointConfiguration
    {
        public string Url { get; set; }
        public string PricesRealm { get; set; }
        public string PricesTopic { get; set; }
    }
}
