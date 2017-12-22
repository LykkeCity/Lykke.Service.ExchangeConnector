using System;
using System.Globalization;

namespace QuickFix.Util
{
    /// <summary>
    /// Utility class for serializing/deserializing a date (which is strangely not-trivial in C#).
    /// Don't use these in your client app.
    /// </summary>
    public static class UtcDateTimeSerializer
    {
        private const string FORMAT = "yyyyMMdd-HH:mm:ss.ffffff K";

        /// <summary>
        /// Not for use by client apps.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string ToString(DateTime d)
        {
            return d.ToString(FORMAT);
        }

        /// <summary>
        /// Not for use by client apps.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DateTime FromString(string s)
        {
            if (!DateTime.TryParseExact(s, FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var d))
            {
                return new DateTime(0L, DateTimeKind.Utc); // MinValue in UTC
            }
            return d;

        }
    }
}
