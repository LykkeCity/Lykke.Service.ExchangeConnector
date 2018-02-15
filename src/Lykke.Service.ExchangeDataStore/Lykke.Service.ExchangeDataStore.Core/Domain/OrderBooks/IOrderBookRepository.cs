using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks
{
    public interface IOrderBookRepository
    {
        Task<List<OrderBook>> GetAsync(string exchangeName, string instrument, DateTime from, DateTime to, CancellationToken cancelToken);
    }
}
