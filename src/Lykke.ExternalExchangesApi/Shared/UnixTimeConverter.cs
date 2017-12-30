using System;
using Microsoft.Rest.Serialization;

namespace Lykke.ExternalExchangesApi.Shared
{
    public static class UnixTimeConverter
    {

        private static readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(DateTime dateTime)
        {
            return (long)dateTime.Subtract(UnixTimeJsonConverter.EpochDate).TotalSeconds;
        }

        public static DateTime FromUnixTime(long seconds)
        {
            return EpochDate.AddSeconds(seconds);
        }

        public static long UnixTimeStampUtc()
        {
            return ToUnixTime(DateTime.UtcNow);
        }
    }
}
