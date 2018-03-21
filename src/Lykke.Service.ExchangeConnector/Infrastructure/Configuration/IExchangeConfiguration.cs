using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public interface IExchangeConfiguration
    {
        bool Enabled { get; set; }

        bool SaveOrderBooksToAzure { get; set; }

        bool PubQuotesToRabbit { get; set; }

        double InitialRating { get; set; }

        /// <summary>
        /// Use SupportedCurrencySymbols as filter of instrument for stream to rabbitmq
        /// true or null - provide only this instrument with mapping name
        /// false - provide all instrument and mapping name use this array
        /// </summary>
        bool? UseSupportedCurrencySymbolsAsFilter { get; set; }

        IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; }
    }
}
