using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class GdaxExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public string[] Instruments { get; set; }

        public bool SaveQuotesToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public string PassPhrase { get; set; }

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string RestEndpointUrl { get; set; }

        public string WssEndpointUrl { get; set; }

        public string UserAgent { get; set; }

        public Dictionary<string, string> CurrencyMapping { get; set; }
    }
}
