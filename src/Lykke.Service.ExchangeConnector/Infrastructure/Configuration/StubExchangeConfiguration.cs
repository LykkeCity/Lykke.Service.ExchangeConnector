using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class StubExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public int PricesIntervalInMilliseconds { get; set; }
        
        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
