using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class OpenOrdersRequest : IKrakenRequest
    {
        public bool Trades { get; set; }
        
        public string UserRef { get; set; }

        public IEnumerable<KeyValuePair<string, string>> FormData
        {
            get
            {
                if (Trades)
                    yield return new KeyValuePair<string, string>("trades", "true");
                
                if (!string.IsNullOrEmpty(UserRef))
                    yield return new KeyValuePair<string, string>("userref", UserRef);
            }
        }
    }
}