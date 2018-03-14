using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;
using System.IO;

namespace TradingBot.Infrastructure.Configuration
{
    public class IcmExchangeConfiguration : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string Password { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public bool SocketConnection { get; set; }

        public RabbitMqExchangeConfiguration RabbitMq { get; set; }
        
        public string[] FixConfiguration { get; set; }

        [Optional]
        public bool? UseSupportedCurrencySymbolsAsFilter { get; set; }
        IReadOnlyCollection<CurrencySymbol> IExchangeConfiguration.SupportedCurrencySymbols => SupportedCurrencySymbols;

        public IReadOnlyCollection<IcmCurrencySymbol> SupportedCurrencySymbols { get; set; }

        public TextReader GetFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", FixConfiguration));
        }
    }
}
