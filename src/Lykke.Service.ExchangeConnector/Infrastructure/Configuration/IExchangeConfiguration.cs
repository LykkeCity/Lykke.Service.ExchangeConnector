using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public interface IExchangeConfiguration
    {
        bool Enabled { get; set; }

        bool SaveQuotesToAzure { get; set; }

        bool SaveOrderBooksToAzure { get; set; }

        bool PubQuotesToRabbit { get; set; }

        double InitialRating { get; set; }

        IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; }
    }
}
