using System;
using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class ClosedOrdersRequest : IKrakenRequest
    {
        public bool Trades { get; set; }
        
        public string UserRef { get; set; }
        
        public long? Start { get; set; }
        
        public long? End { get; set; }

        public IEnumerable<KeyValuePair<string, string>> FormData
        {
            get
            {
                if (Trades)
                    yield return new KeyValuePair<string, string>("trades", "true");
                
                if (!string.IsNullOrEmpty(UserRef))
                    yield return new KeyValuePair<string, string>("userref", UserRef);
                
                if (Start.HasValue)
                    yield return new KeyValuePair<string, string>("start", Start.Value.ToString());
                
                if (End.HasValue)
                    yield return new KeyValuePair<string, string>("start", End.Value.ToString());
            }
        }
    }
}
