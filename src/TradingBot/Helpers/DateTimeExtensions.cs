using System;

namespace TradingBot.Helpers
{
    public static class DateTimeUtils
    {
        public static DateTime FromUnix(long unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixtime);
        }
    }
}
