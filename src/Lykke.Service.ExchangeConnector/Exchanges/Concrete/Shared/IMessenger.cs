using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal interface IMessenger<in TRequest, TResponse> : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SendRequestAsync(TRequest request, CancellationToken cancellationToken);
        Task<TResponse> GetResponseAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
