using System;

namespace TradingBot.Exchanges.Concrete.Kraken.Responses
{
    public class ServerTimeResult
    {
        public long UnixTime { get; set; }

        public DateTime Rfc1123 { get; set; }
    }
}
