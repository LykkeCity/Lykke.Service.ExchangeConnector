using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class OandaConfiguration : IExchangeConfiguration
    {
        public OandaConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        [Lykke.SettingsReader.Attributes.Optional]
        public bool? UseSupportedCurrencySymbolsAsFilter { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
