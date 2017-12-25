using System;
using System.Collections.Generic;

namespace TradingBot.Infrastructure.Configuration
{
    public class HistoricalDataConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public string BaseDirectory { get; set; }
        
        public string FileName { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }
    }
}
