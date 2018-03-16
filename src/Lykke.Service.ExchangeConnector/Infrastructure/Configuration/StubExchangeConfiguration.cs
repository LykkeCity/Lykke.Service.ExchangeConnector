using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class StubExchangeConfiguration : IExchangeConfiguration
    {
        public StubExchangeConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }


        [Lykke.SettingsReader.Attributes.Optional]
        public bool UseSupportedCurrencySymbolsAsFilter { get; set; }

        public int PricesIntervalInMilliseconds { get; set; }


        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
