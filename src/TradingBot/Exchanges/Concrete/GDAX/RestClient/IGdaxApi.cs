using System;
using System.Threading;
using System.Threading.Tasks;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal interface IGdaxApi : IDisposable
    {
        Task<object> AddOrder(string symbol, decimal amount, decimal price, string side, 
            string type, CancellationToken cancellationToken = default);
        Task<object> CancelOrder(long orderId, CancellationToken cancellationToken = default);
        Task<object> GetOpenOrders(CancellationToken cancellationToken = default);
        Task<object> GetOrderStatus(long orderId, CancellationToken cancellationToken = default);
        Task<object> GetBalances(CancellationToken cancellationToken = default);
        Task<object> GetMarginInformation(CancellationToken cancellationToken = default);
    }
}
