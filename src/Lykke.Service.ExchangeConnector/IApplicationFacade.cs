using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot
{
    public interface IApplicationFacade : IStopable
    {
        IReadOnlyCollection<IExchange> GetExchanges();

        IExchange GetExchange(string name);

        Task Start();

    }
}
