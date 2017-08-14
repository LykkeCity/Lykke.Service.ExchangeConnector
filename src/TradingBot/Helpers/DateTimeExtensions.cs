using System;

namespace TradingBot.Helpers
{
    public static class DateTimeUtils
    {
        private static DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromUnix(long unixtime)
        {
            return BaseTime.AddSeconds(unixtime);
        }

        public static DateTime FromUnix(double unixtime)
        {
            return BaseTime.AddSeconds(unixtime);
        }

        public static DateTime FromUnix(decimal unixtime)
        {
            long baseTicks = BaseTime.Ticks;
            long ticks = decimal.ToInt64(unixtime * 10000000);
            return new DateTime(ticks + baseTicks);
        }

        public static DateTime TruncSeconds(this DateTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        }
    }
}
