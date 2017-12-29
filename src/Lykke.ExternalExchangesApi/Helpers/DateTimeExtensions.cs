using System;

namespace Lykke.ExternalExchangesApi.Helpers
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
