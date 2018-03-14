using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public interface IExchangeConfiguration
    {
        bool Enabled { get; set; }

        bool PubQuotesToRabbit { get; set; }
       
        bool? UseSupportedCurrencySymbolsAsFilter { get; set; }

        IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; }
    }
}
