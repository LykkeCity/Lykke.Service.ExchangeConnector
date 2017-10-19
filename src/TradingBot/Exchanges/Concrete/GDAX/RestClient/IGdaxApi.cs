using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal interface IGdaxApi : IDisposable
    {
        Task<GdaxOrderResponse> AddOrder(string symbol, decimal amount, decimal price, GdaxOrderSide side, 
            GdaxOrderType type, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Guid>> CancelOrder(Guid orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GdaxOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default);
        Task<GdaxOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<GdaxMarginInfoResponse>> GetMarginInformation(CancellationToken cancellationToken = default);
    }
}
