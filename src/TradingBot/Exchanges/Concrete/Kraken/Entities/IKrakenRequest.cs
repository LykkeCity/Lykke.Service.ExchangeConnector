using System.Collections.Generic;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public interface IKrakenRequest
    {
        IEnumerable<KeyValuePair<string, string>> FormData { get; }
    }
}