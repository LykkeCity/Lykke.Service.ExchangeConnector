using System.IO;

namespace TradingBot.Infrastructure.Configuration
{
    public class IcmConfig : IExchangeConfiguration
    {
        public bool Enabled { get; set; }
        
        public string Username { get; set; }

        public string Password { get; set; }

        public string[] Instruments { get; set; }
        
        public bool SaveQuotesToAzure { get; set; }
        
        public bool PubQuotesToRabbit { get; set; }

        public double InitialRating { get; set; }

        public bool SocketConnection { get; set; }

        public IcmRabbitMqConfiguration RabbitMq { get; set; }
        
        public string[] FixConfiguration { get; set; }

        public TextReader GetFixConfigAsReader()
        {
            return new StringReader(string.Join("\n", FixConfiguration));
        }
    }
}
