using Lykke.Service.ExchangeDataStore.Core.Domain;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Services.OrderBooks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Services.Domain
{
    // ReSharper disable once ClassNeverInstantiated.Global - Autofac instantiated
    public class OrderBookService : IOrderBookService, IDisposable
    {
        private readonly IOrderBookRepository _orderBookRepo;
        private readonly CancellationTokenSource _cancellationSource;

        public OrderBookService(IOrderBookRepository orderBookRepo)
        {
            _orderBookRepo = orderBookRepo;
            _cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(Constants.BlobStorageTimeOutSeconds));
        }

        public async Task<List<OrderBook>> GetAsync(string exchangeName, string instrument, DateTime from, DateTime to)
        {
            var result = await _orderBookRepo.GetAsync(exchangeName, instrument, from, to, _cancellationSource.Token);
            return result;
        }

        public void Dispose()
        {
            _cancellationSource?.Cancel();
            _cancellationSource?.Dispose();
        }
    }
}
