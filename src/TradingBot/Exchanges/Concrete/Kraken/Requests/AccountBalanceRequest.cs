using System.Collections.Generic;
using System.Linq;
using TradingBot.Exchanges.Concrete.Kraken.Entities;

namespace TradingBot.Exchanges.Concrete.Kraken.Requests
{
    public class AccountBalanceRequest : IKrakenRequest
    {
        public IEnumerable<KeyValuePair<string, string>> FormData => Enumerable.Empty<KeyValuePair<string, string>>();
    }
}