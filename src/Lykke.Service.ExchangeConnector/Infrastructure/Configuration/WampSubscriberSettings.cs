using System;

namespace TradingBot.Infrastructure.Configuration
{
    public class WampSubscriberSettings
    {
        public string Address { get; set; }
        public string Realm { get; set; }
        public int OpenTimeout { get; set; } = 5000;
        public string[] Topics { get; set; } = new string[0];
    }
}
