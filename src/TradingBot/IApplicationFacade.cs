using System.Collections.Generic;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot
{
    public interface IApplicationFacade
    {
        IReadOnlyCollection<string> GetConnectedExchanges();

        Exchange GetExchange(string name);
        
        TranslatedSignalsRepository TranslatedSignalsRepository { get; }
    }
}