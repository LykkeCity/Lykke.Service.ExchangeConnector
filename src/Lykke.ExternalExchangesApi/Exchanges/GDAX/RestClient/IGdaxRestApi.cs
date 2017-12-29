using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions.Models;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient
{
    public interface IGdaxRestApi : IDisposable
    {
        Task<GdaxOrderResponse> AddOrder(string symbol, decimal amount, decimal price, GdaxOrderSide side, 
            GdaxOrderType type, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<bool> CancelOrder(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<IReadOnlyList<GdaxOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<GdaxOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
    }
}
