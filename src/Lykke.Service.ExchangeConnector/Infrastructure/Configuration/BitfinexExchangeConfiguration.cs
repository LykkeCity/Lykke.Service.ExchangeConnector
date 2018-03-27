using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class BitfinexExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        [Optional]
        public bool EnableExecution { get; set; } = true;


        [Optional]
        public bool UseSupportedCurrencySymbolsAsFilter { get; set; } = true;

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string EndpointUrl { get; set; }

        public string WebSocketEndpointUrl { get; set; }

        public int MaxOrderBookRate { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
