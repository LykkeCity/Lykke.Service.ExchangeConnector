using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class OandaConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
