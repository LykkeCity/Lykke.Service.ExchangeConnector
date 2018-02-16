using Lykke.Service.ExchangeDataStore.Core.Domain;
using Lykke.Service.ExchangeDataStore.Core.Domain.OrderBooks;
using Lykke.Service.ExchangeDataStore.Core.Domain.Ticks;
using Lykke.Service.ExchangeDataStore.Core.Services.OrderBooks;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<TickPrice>> GetTickPricesAsync(string exchangeName, string instrument, DateTime from, DateTime to)
        {
            var orderBooks = (await _orderBookRepo.GetAsync(exchangeName, instrument, from, to, _cancellationSource.Token)).OrderBy(o=>o.Timestamp);

            var result = new List<TickPrice>();
            var tickInstrument = new Instrument(exchangeName, instrument);

            orderBooks.ForEach(orderBook =>
            {
                var lowestAsk = GetLowestAsk(orderBook);
                var highestBid = GetHighestBid(orderBook);

                var latestTick = GetCurrentLatestTickPrice(result);

                if (!result.Any() || IsBidOrAskDifferentInLatestTick(latestTick, lowestAsk, highestBid))
                {
                    result.Add(new TickPrice(tickInstrument, orderBook.Timestamp, lowestAsk, highestBid));
                }
            });

            return result;
        }

        private bool IsBidOrAskDifferentInLatestTick(TickPrice currentLatestTick, decimal lowestAsk, decimal highestBid)
        {
            return currentLatestTick?.Ask != lowestAsk || currentLatestTick?.Bid != highestBid;
        }

        private TickPrice GetCurrentLatestTickPrice(List<TickPrice> result)
        {
            return result.Any() ? result.MaxBy(r => r.Time.Ticks) : null;
        }

        private decimal GetLowestAsk(OrderBook orderBook)
        {
            return orderBook.Asks.Any() ? orderBook.Asks.Min(a => a.Price) : 0;
        }
        private decimal GetHighestBid(OrderBook orderBook)
        {
            return orderBook.Bids.Any() ? orderBook.Bids.Max(a => a.Price) : 0;
        }

        public void Dispose()
        {
            _cancellationSource?.Cancel();
            _cancellationSource?.Dispose();
        }
    }
}
