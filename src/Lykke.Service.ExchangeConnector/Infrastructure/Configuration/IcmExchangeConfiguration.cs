using System.Collections.Generic;
using System.IO;

namespace TradingBot.Infrastructure.Configuration
{
    public class IcmExchangeConfiguration : IExchangeConfiguration
    {
        public IcmExchangeConfiguration()
        {
            UseSupportedCurrencySymbolsAsFilter = true;
        }

        public bool Enabled { get; set; }
        
        public string Password { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        [Lykke.SettingsReader.Attributes.Optional]
        public bool UseSupportedCurrencySymbolsAsFilter { get; set; }

        public bool SocketConnection { get; set; }

        public RabbitMqExchangeConfiguration RabbitMq { get; set; }
        
        public string[] FixConfiguration { get; set; }

        IReadOnlyCollection<CurrencySymbol> IExchangeConfiguration.SupportedCurrencySymbols => SupportedCurrencySymbols;

        public IReadOnlyCollection<IcmCurrencySymbol> SupportedCurrencySymbols { get; set; }

        public TextReader GetFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", FixConfiguration));
        }
    }
}
