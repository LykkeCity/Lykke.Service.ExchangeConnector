using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Services.OrderBooks
{
    public interface IOrderBookService
    {
        Task<List<OrderBook>> GetAsync(string exchangeName, string instrument, DateTime from, DateTime to);
    }
}
