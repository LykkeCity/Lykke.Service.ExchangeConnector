using System.Collections.Generic;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex
{
    public static class ListExt
    {
        public static void Add(this List<KeyValuePair<string, string>> list, string key, object value)
        {
            if (value != null)
            {
                list.Add(new KeyValuePair<string, string>(key, value.ToString()));
            }
        }
    }
}
