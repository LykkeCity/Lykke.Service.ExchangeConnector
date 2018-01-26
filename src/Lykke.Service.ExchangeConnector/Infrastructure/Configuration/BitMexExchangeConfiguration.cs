using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class BitMexExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string EndpointUrl { get; set; }

        public string WebSocketEndpointUrl { get; set; }

        public int MaxOrderBookRate { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
