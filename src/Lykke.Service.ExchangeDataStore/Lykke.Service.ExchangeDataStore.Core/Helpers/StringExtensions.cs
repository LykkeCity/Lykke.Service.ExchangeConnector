using Lykke.Service.ExchangeDataStore.Core.Domain;
using System;
using System.Globalization;
using System.Linq;

namespace Lykke.Service.ExchangeDataStore.Core.Helpers
{
    public static class StringExtensions
    {
        public static string RemoveSpecialCharacters(this string str, params char[] additionalCharsAllowed)
        {
            return new string(str.Where(c => char.IsLetterOrDigit(c) ||
                                             additionalCharsAllowed.Contains(c)).ToArray());
        }

        public static DateTime ParseOrderbookTimestamp(this string str)
        {
            return DateTime.ParseExact(str.Substring(0, 19), Constants.OrderbookTimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }
    }
}
