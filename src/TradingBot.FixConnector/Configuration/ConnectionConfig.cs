using System;
using System.Collections.Generic;

namespace TradingBot.FixConnector.Configuration
{
    public class ConnectionConfig
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Instruments { get; set; }
    }
}
