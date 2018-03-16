using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class KrakenConfig : IExchangeConfiguration
    {
        public KrakenConfig()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }


        [Lykke.SettingsReader.Attributes.Optional]
        public bool UseSupportedCurrencySymbolsAsFilter { get; set; }

        public string ApiKey { get; set; }
        
        public string PrivateKey { get; set; }


        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
