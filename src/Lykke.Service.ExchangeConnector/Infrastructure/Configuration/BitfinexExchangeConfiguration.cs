using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class BitfinexExchangeConfiguration : IExchangeConfiguration, ICurrencyMappingProvider
    {
        public bool Enabled { get; set; }

        public string[] Instruments { get; set; }

        public bool SaveQuotesToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string EndpointUrl { get; set; }

        public string WebSocketEndpointUrl { get; set; }

        public int MaxOrderBookRate { get; set; }

        public IDictionary<string, string> CurrencyMapping { get; set; }
    }
}
