using Lykke.Service.ExchangeDataStore.Core.Domain;
using System;

namespace Lykke.Service.ExchangeDataStore.Core.Helpers
{
    public static class DateTimeExtensions
    {
        public static string ToSnapshotTimestampFormat(this DateTime dateTime)
        {
            return dateTime.ToString(Constants.OrderbookTimestampFormat);
        }
    }
}
