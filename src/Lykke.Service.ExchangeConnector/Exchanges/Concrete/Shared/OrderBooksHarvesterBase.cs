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
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken CancellationToken;
        private DateTime _lastPublishTime = DateTime.MinValue;
        private long _lastSecPublicationsNum;
        private int _orderBooksReceivedInLastTimeFrame;
        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod = TimeSpan.FromSeconds(10);
        private Task _measureTask;
        private long _publishedToRabbit;

        protected OrderBooksHarvesterBase(ICurrencyMappingProvider currencyMappingProvider, string uri, ILog log)
        {
            Log = log.CreateComponentScope(GetType().Name);
            Messenger = new WebSocketTextMessenger(uri, Log, CancellationToken);
            OrderBookSnapshot = new HashSet<OrderBookItem>();
            CurrencyMappingProvider = currencyMappingProvider;
            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private async void ForceStopMessenger(object state)
        {
            await Log.WriteWarningAsync(nameof(ForceStopMessenger), "Monitoring heartbeat", $"Heart stopped. Restarting {GetType().Name}");
            Stop();
            try
            {
                await _messageLoopTask;
            }
            catch (OperationCanceledException)
            {

            }
            Start();
        }

        protected void RechargeHeartbeat()
        {
            _heartBeatMonitoringTimer.Change(_heartBeatPeriod, Timeout.InfiniteTimeSpan);
        }

        private async Task Measure()
        {
            const double period = 60;
            while (true)
            {
                var msgInSec = _lastSecPublicationsNum / period;
                var pubInSec = _publishedToRabbit / period;
                await Log.WriteInfoAsync(nameof(OrderBooksHarvesterBase), $"Receive rate from {ExchangeName} {msgInSec} per second, publish rate to RabbitMq {pubInSec} per second", string.Empty);
                _lastSecPublicationsNum = 0;
                _publishedToRabbit = 0;
                await Task.Delay(TimeSpan.FromSeconds(period), CancellationToken);
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
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _messageLoopTask = MessageLoop();
            _measureTask = Measure();
        }

        public void Stop()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").Wait();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private async Task MessageLoop()
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
            _lastSecPublicationsNum++;
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
                _publishedToRabbit++;
            }
        }

        private bool NeedThrottle()
        {
            var result = false;
            if (MaxOrderBookRate == 0)
            {
                return true;
            }
            if (_orderBooksReceivedInLastTimeFrame >= MaxOrderBookRate)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastPublishTime).TotalSeconds >= 1)
                {
                    _orderBooksReceivedInLastTimeFrame = 0;
                    _lastPublishTime = now;
                }
                else
                {
                    result = true;
                }
            }
            _orderBooksReceivedInLastTimeFrame++;
            return result;
        }

        protected abstract Task MessageLoopImpl();

        public virtual void Dispose()
        {
            Stop();
            Messenger?.Dispose();
            _messageLoopTask?.Dispose();
            _cancellationTokenSource?.Dispose();
            _heartBeatMonitoringTimer?.Dispose();
            _measureTask?.Dispose();
        }


    }
}
