using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class BitstampConfiguration : IExchangeConfiguration
    {
        public BitstampConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }
        public bool SaveOrderBooksToAzure { get; set; }
        public bool PubQuotesToRabbit { get; set; }
        public double InitialRating { get; set; }
        [Lykke.SettingsReader.Attributes.Optional]
        public bool UseSupportedCurrencySymbolsAsFilter { get; set; }
        public string ApplicationKey { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
