using System.Collections.Generic;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Responses
{
    public class ClosedOrdersResponse
    {
        public Dictionary<string, OrderInfo> Closed { get; set; }
        
        public int Count { get; set; }
    }
}
