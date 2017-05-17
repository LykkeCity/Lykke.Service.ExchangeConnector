using System;
using TradingBot.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class ServerTime
    {
        public long UnixTime { get; set; }

        public DateTime Rfc1123 { get; set; }

        public DateTime FromUnixTime => DateTimeUtils.FromUnix(UnixTime);
    }
}
