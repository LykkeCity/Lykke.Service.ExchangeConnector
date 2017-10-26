using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot
{
    public interface IApplicationFacade : IDisposable
    {
        IReadOnlyCollection<IExchange> GetExchanges();

        IExchange GetExchange(string name);

        TranslatedSignalsRepository TranslatedSignalsRepository { get; }
        Task Start();
        void Stop();
    }
}
