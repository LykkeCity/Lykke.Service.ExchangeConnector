using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class OandaConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        [Optional]
        public bool? UseSupportedCurrencySymbolsAsFilter { get; set; }
        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
