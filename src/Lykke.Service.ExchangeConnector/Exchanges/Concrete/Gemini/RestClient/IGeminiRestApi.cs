using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient
{
    internal interface IGeminiRestApi : IDisposable
    {
        Task<GeminiOrderResponse> AddOrder(string symbol, decimal amount, decimal price, GeminiOrderSide side, 
            GeminiOrderType type, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<IReadOnlyCollection<Guid>> CancelOrder(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<IReadOnlyList<GeminiOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<GeminiOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
        Task<IReadOnlyList<GeminiBalanceResponse>> GetBalances(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default);
    }
}
