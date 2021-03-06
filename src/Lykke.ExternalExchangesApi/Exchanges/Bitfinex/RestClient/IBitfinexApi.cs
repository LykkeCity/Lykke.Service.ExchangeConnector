﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient
{
    public interface IBitfinexApi : IDisposable
    {
        Task<object> AddOrder(string symbol, decimal amount, decimal price, string side, string type, CancellationToken cancellationToken = default);
        Task<object> CancelOrder(long orderId, CancellationToken cancellationToken = default);
        Task<object> GetActiveOrders(CancellationToken cancellationToken = default);
        Task<object> GetOrderStatus(long orderId, CancellationToken cancellationToken = default);
        Task<object> GetBalances(CancellationToken cancellationToken = default);
        Task<object> GetMarginInformation(CancellationToken cancellationToken = default);
        Task<object> GetActivePositions(CancellationToken cancellationToken = default);
        Task<object> GetAllSymbols(CancellationToken cancellationToken = default);
    }
}
