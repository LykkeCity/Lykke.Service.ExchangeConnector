using System;

namespace TradingBot.Infrastructure.Configuration
{
    public class AspNetConfiguration
    {
        public string Host { get; set; }
        
        public string ApiKey { get; set; }
        
        public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}