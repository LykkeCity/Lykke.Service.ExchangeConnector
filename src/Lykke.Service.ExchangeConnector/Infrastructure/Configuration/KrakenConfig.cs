using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class KrakenConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public string ApiKey { get; set; }
        
        public string PrivateKey { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
