using System;
using Lykke.ExternalExchangesApi.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class ServerTime
    {
        public long UnixTime { get; set; }

        public DateTime Rfc1123 { get; set; }

        public DateTime FromUnixTime => DateTimeUtils.FromUnix(UnixTime);
    }
}
