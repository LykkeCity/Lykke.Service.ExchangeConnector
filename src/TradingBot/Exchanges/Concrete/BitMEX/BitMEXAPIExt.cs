using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.AutorestClient
{
    public partial class BitMEXAPI
    {
        
    }

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
