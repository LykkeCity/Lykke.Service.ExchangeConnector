using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Polly;
using TradingBot.Exchanges.Concrete.BitMEX;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksHarvesterBase : IDisposable
    {
        protected readonly ILog Log;
        protected readonly WebSocketTextMessenger Messenger;
        protected readonly ISet<OrderBookItem> OrderBookSnapshot;
        private Task _messageLoopTask;
        private Func<OrderBook, Task> _newOrderBookHandler;
        protected ICurrencyMappingProvider CurrencyMappingProvider { get; }
        private readonly CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken CancellationToken;
        private DateTime _lastPublishTime = DateTime.UtcNow;
        private long _lastSecPublicationsNum;
        private int _currentPublicationsNum;
        private long _currentPublicationsNumPerfCounter;

        protected OrderBooksHarvesterBase(ICurrencyMappingProvider currencyMappingProvider, string uri, ILog log)
        {
            Log = log.CreateComponentScope(GetType().Name);
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            Messenger = new WebSocketTextMessenger(uri, Log, CancellationToken);
            OrderBookSnapshot = new HashSet<OrderBookItem>();
            CurrencyMappingProvider = currencyMappingProvider;
            new Task(Measure).Start();
        }

        private async void Measure()
        {
            while (true)
            {
                var msgInSec = (_currentPublicationsNumPerfCounter - _lastSecPublicationsNum) / 10d;
                await Log.WriteInfoAsync(nameof(OrderBooksHarvesterBase), $"Order books received from {ExchangeName} in 1 sec", msgInSec.ToString());
                _lastSecPublicationsNum = _currentPublicationsNumPerfCounter;
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        public string ExchangeName { get; set; }

        public int MaxOrderBookRate { get; set; }

        public void AddHandler(Func<OrderBook, Task> handler)
        {
            _newOrderBookHandler = handler;
        }

        public void Start()
        {
            _messageLoopTask = new Task(MessageLoop);
            _messageLoopTask.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async void MessageLoop()
        {
            const int smallTimeout = 5;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryForeverAsync(attempt => attempt % 60 == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout)); // After every 60 attempts wait 5min 

            await retryPolicy.ExecuteAsync(async () =>
            {
                await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                try
                {
                    await MessageLoopImpl();
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(MessageLoopImpl), $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                    throw;
                }
            });

        }

        protected async Task PublishOrderBookSnapshotAsync()
        {
            _currentPublicationsNumPerfCounter++;
            if (NeedThrottle())
            {
                return;
            }
            var orderBooks = from si in OrderBookSnapshot
                             group si by si.Symbol into g
                             let asks = g.Where(i => !i.IsBuy).Select(i => new VolumePrice(i.Price, i.Size)).ToArray()
                             let bids = g.Where(i => i.IsBuy).Select(i => new VolumePrice(i.Price, i.Size)).ToArray()
                             let assetPair = BitMexModelConverter.ConvertSymbolFromBitMexToLykke(g.Key, CurrencyMappingProvider).Name
                             select new OrderBook(ExchangeName, assetPair, asks, bids, DateTime.UtcNow);

            foreach (var orderBook in orderBooks)
            {
                await _newOrderBookHandler(orderBook);
            }
        }

        private bool NeedThrottle()
        {
            var result = false;
            if (_currentPublicationsNum >= MaxOrderBookRate)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastPublishTime).TotalSeconds > 1)
                {
                    _currentPublicationsNum = 0;
                    _lastPublishTime = now;
                }
                else
                {
                    result = true;
                }
            }
            _currentPublicationsNum++;
            return result;
        }

        protected abstract Task MessageLoopImpl();

        public void Dispose()
        {
            Stop();
            Messenger?.Dispose();
            _messageLoopTask?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
