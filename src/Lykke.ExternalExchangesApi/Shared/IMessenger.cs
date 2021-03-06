﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.ExternalExchangesApi.Shared
{
    public interface IMessenger<in TRequest, TResponse> : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SendRequestAsync(TRequest request, CancellationToken cancellationToken);
        Task<TResponse> GetResponseAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
