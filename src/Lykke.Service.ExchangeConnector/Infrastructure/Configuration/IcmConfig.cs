using System.Collections.Generic;
using System.IO;

namespace TradingBot.Infrastructure.Configuration
{
    public class IcmConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string Username { get; set; }

        public string Password { get; set; }

        public bool SaveOrderBooksToAzure { get; set; }

        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public bool SocketConnection { get; set; }

        public RabbitMqExchangeConfiguration RabbitMq { get; set; }
        
        public string[] FixConfiguration { get; set; }

        public IReadOnlyCollection<CurrencySymbol> SupportedCurrencySymbols { get; set; }

        public TextReader GetFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", FixConfiguration));
        }
    }
}
