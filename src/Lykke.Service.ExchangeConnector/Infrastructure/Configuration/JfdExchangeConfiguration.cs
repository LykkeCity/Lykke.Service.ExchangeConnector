using System.Collections.Generic;
using System.IO;

namespace TradingBot.Infrastructure.Configuration
{
    public sealed class JfdExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public string Password { get; set; }

        public bool SaveQuotesToAzure { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }

        public string[] TradingFixConfiguration { get; set; }

        public string[] QuotingFixConfiguration { get; set; }

        public int MaxOrderBookRate { get; set; }


        public TextReader GetTradingFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", TradingFixConfiguration));
        }

        public TextReader GetQuotingFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", QuotingFixConfiguration));
        }
    }
}
