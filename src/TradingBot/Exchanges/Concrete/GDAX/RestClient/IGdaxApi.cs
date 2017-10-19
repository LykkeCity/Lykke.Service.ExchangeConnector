using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal interface IGdaxApi : IDisposable
    {
        Task<GdaxOrder> AddOrder(string symbol, decimal amount, decimal price, string side, 
            string type, CancellationToken cancellationToken = default);
        Task<GdaxOrder> CancelOrder(Guid orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GdaxOrder>> GetOpenOrders(CancellationToken cancellationToken = default);
        Task<GdaxOrder> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<GdaxMarginInfoResponse>> GetMarginInformation(CancellationToken cancellationToken = default);
    }
}
