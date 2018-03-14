using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class BitfinexExchangeConfiguration : IExchangeConfiguration
    {
        public BitfinexExchangeConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        [Lykke.SettingsReader.Attributes.Optional]
        public bool? UseSupportedCurrencySymbolsAsFilter { get; set; }

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string EndpointUrl { get; set; }

        public string WebSocketEndpointUrl { get; set; }

        public int MaxOrderBookRate { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
