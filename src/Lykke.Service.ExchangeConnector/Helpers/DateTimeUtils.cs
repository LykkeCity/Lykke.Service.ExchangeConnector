using System;

namespace TradingBot.Helpers
{
    public static class DateTimeUtils
    {
        internal static readonly DateTime BaseUnixDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnix(long unixtime)
        {
            return BaseUnixDateTime.AddSeconds(unixtime);
        }

        public static DateTime FromUnix(double unixtime)
        {
            return BaseUnixDateTime.AddSeconds(unixtime);
        }

        public static DateTime FromUnix(decimal unixtime)
        {
            var baseTicks = BaseUnixDateTime.Ticks;
            var ticks = decimal.ToInt64(unixtime * 10000000);
            return new DateTime(ticks + baseTicks);
        }

        public static long ToUnix(DateTime time)
        {
            return (time.Ticks - BaseUnixDateTime.Ticks) / 10000000L;
        }

        public static DateTime TruncSeconds(this DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        }
    }
}
