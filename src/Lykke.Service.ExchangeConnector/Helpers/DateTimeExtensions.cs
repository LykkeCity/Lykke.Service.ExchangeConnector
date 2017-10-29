using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradingBot.Helpers
{
    public static class DateTimeExtensions
    {
        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (dateTime - DateTimeUtils.BaseUnixDateTime).TotalSeconds;
        }

        public static int ToUnixTimestampInt(this DateTime dateTime)
        {
            return (int)ToUnixTimestamp(dateTime);
        }
    }
}
