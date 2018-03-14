using Common.Log;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksHarvesterBase : IDisposable
    {
        protected readonly ILog Log;
        protected CancellationToken CancellationToken;

        private readonly ConcurrentDictionary<string, OrderBookSnapshot> _orderBookSnapshots;
        private readonly ExchangeConverters _converters;
        private readonly Timer _heartBeatMonitoringTimer;
        protected TimeSpan HeartBeatPeriod { get; set; } = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _snapshotRefreshPeriod = TimeSpan.FromSeconds(5);
        private CancellationTokenSource _cancellationTokenSource;
        private Task _messageLoopTask;
        private readonly IHandler<OrderBook> _newOrderBookHandler;
        private DateTime _lastPublishTime = DateTime.MinValue;
        private long _lastSecPublicationsNum;
        private int _orderBooksReceivedInLastTimeFrame;
        private Task _measureTask;
        private long _publishedToRabbit;
        private readonly Timer _snapshotRefreshTimer;
        private volatile bool _restartInProgress;
        private volatile bool _snapshotRefreshScheduled;

        protected IExchangeConfiguration ExchangeConfiguration { get; }

        public string ExchangeName { get; }

        public int MaxOrderBookRate { get; set; }

        protected OrderBooksHarvesterBase(string exchangeName, IExchangeConfiguration exchangeConfiguration, ILog log,
            IHandler<OrderBook> newOrderBookHandler)
        {
            ExchangeConfiguration = exchangeConfiguration;
            _newOrderBookHandler = newOrderBookHandler;
            ExchangeName = exchangeName;

            Log = log.CreateComponentScope(GetType().Name);

            _converters = new ExchangeConverters(exchangeConfiguration.SupportedCurrencySymbols,
                string.Empty);

            _orderBookSnapshots = new ConcurrentDictionary<string, OrderBookSnapshot>();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            _heartBeatMonitoringTimer = new Timer(s => RestartMessenger("No messages from the exchange"));
            _snapshotRefreshTimer = new Timer(s => RestartMessenger("Refresh order book snapshot"));
        }

        private void RestartMessenger(string reason)
        {
            if (_restartInProgress)
            {
                return;
            }

            _restartInProgress = true;
            _snapshotRefreshScheduled = false;

            try
            {
                Log.WriteWarningAsync(nameof(RestartMessenger), string.Empty, $"Restart requested. The reason: {reason}. Restarting {GetType().Name}").GetAwaiter().GetResult();
                Stop();
                Start();
            }
            finally
            {
                _restartInProgress = false;
            }
        }



        protected void RechargeHeartbeat()
        {
            _heartBeatMonitoringTimer.Change(HeartBeatPeriod, Timeout.InfiniteTimeSpan);
        }

        private async Task Measure()
        {
            const double period = 60;
            while (!CancellationToken.IsCancellationRequested)
            {
                var msgInSec = _lastSecPublicationsNum / period;
                var pubInSec = _publishedToRabbit / period;
                await Log.WriteInfoAsync(nameof(OrderBooksHarvesterBase),
                    $"Receive rate from {ExchangeName} {msgInSec} per second, publish rate to " +
                    $"RabbitMq {pubInSec} per second", string.Empty);
                _lastSecPublicationsNum = 0;
                _publishedToRabbit = 0;
                await Task.Delay(TimeSpan.FromSeconds(period), CancellationToken).ConfigureAwait(false);
            }
        }

        public virtual void Start()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").GetAwaiter().GetResult();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _measureTask?.Dispose();
            _measureTask = Task.Run(Measure, CancellationToken);
            StartReading();
        }

        protected virtual void StartReading()
        {
            _messageLoopTask?.Dispose();
            _messageLoopTask = Task.Run(MessageLoop, CancellationToken);
        }

        public virtual void Stop()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").GetAwaiter().GetResult();
            _cancellationTokenSource?.Cancel();
            SwallowException(() => _messageLoopTask?.GetAwaiter().GetResult());
            SwallowException(() => _measureTask?.GetAwaiter().GetResult());
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            CancelSnapshotRefresh();
        }

        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            const int maxAttemptsBeforeLogError = 20;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !CancellationToken.IsCancellationRequested)
                .WaitAndRetryForeverAsync(attempt =>
                {
                    if (attempt == 1)
                    {
                        Log.WriteWarningAsync(nameof(OrderBooksHarvesterBase), "Receiving messages from the socket", "Unable to establish connection with server. Will try in 5 sec. ").GetAwaiter().GetResult();
                    }

                    if (attempt % maxAttemptsBeforeLogError == 0)
                    {
                        Log.WriteErrorAsync(nameof(OrderBooksHarvesterBase), "Receiving messages from the socket", new Exception($"Unable to recover the connection after { maxAttemptsBeforeLogError } attempts. Will try in 5 min.")).GetAwaiter().GetResult();
                    }
                    return attempt % maxAttemptsBeforeLogError == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(smallTimeout);
                }); // After every maxAttemptsBeforeLogError attempts wait 5min 

            await retryPolicy.ExecuteAsync(async () =>
             {
                 await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                 try
                 {
                     await MessageLoopImpl();
                 }
                 catch (OperationCanceledException)
                 {
                     throw;
                 }
                 catch (Exception ex)
                 {
                     await Log.WriteErrorAsync(nameof(MessageLoopImpl),
                         $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                     throw;
                 }
             });
        }

        private async Task PublishOrderBookSnapshotAsync()
        {
            _lastSecPublicationsNum++;
            if (NeedThrottle())
            {
                return;
            }
            var orderBooks = _orderBookSnapshots.Values
                .Select(obs => new OrderBook(
                    ExchangeName,
                    _converters.ExchangeSymbolToLykkeInstrument(obs.AssetPair).Name,
                    obs.Asks.Values.Select(i => new VolumePrice(i.Price, i.Size)).ToArray(),
                    obs.Bids.Values.Select(i => new VolumePrice(i.Price, i.Size)).ToArray(),
                    DateTime.UtcNow));
            _publishedToRabbit++;

            foreach (var orderBook in orderBooks)
            {
                if (orderBook.Asks.Any() || orderBook.Bids.Any())
                {
                    await _newOrderBookHandler.Handle(orderBook);
                }
                else
                {
                    await Log.WriteInfoAsync(nameof(PublishOrderBookSnapshotAsync), $"Source: {orderBook.Source} AssetPairId: {orderBook.AssetPairId}", "skip empty order book");
                }
            }
        }

        protected abstract Task MessageLoopImpl();

        private async Task<OrderBookSnapshot> GetOrderBookSnapshot(string pair)
        {
            if (!_orderBookSnapshots.TryGetValue(pair, out var orderBook))
            {
                var message = "Trying to retrieve a non-existing pair order book snapshot " +
                              $"for exchange {ExchangeName} and pair {pair}";
                await Log.WriteErrorAsync(nameof(MessageLoopImpl), nameof(MessageLoopImpl),
                    new OrderBookInconsistencyException(message));
                throw new OrderBookInconsistencyException(message);
            }

            return orderBook;
        }

        protected bool TryGetOrderBookSnapshot(string pair, out OrderBookSnapshot orderBookSnapshot)
        {
            return _orderBookSnapshots.TryGetValue(pair, out orderBookSnapshot);
        }

        protected async Task HandleOrderBookSnapshotAsync(string pair, DateTime timeStamp, IEnumerable<OrderBookItem> orders)
        {
            var orderBookSnapshot = new OrderBookSnapshot(ExchangeName, pair, timeStamp, Log, ExchangeConfiguration.SupportedCurrencySymbols);
            orderBookSnapshot.AddOrUpdateOrders(orders);
            if (await orderBookSnapshot.DetectNegativeSpread())
            {
                ScheduleSnapshotRefresh();
            }
            else
            {
                CancelSnapshotRefresh();
            }

            _orderBookSnapshots[pair] = orderBookSnapshot;

            await PublishOrderBookSnapshotAsync();
        }

        protected async Task HandleOrdersEventsAsync(string pair,
            OrderBookEventType orderEventType,
            IReadOnlyCollection<OrderBookItem> orders)
        {
            var orderBookSnapshot = await GetOrderBookSnapshot(pair);

            switch (orderEventType)
            {
                case OrderBookEventType.Add:
                case OrderBookEventType.Update:
                    orderBookSnapshot.AddOrUpdateOrders(orders);
                    break;
                case OrderBookEventType.Delete:
                    orderBookSnapshot.DeleteOrders(orders);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderEventType), orderEventType, null);
            }

            if (await orderBookSnapshot.DetectNegativeSpread())
            {
                ScheduleSnapshotRefresh();
            }
            else
            {
                CancelSnapshotRefresh();
            }

            await PublishOrderBookSnapshotAsync();
        }

        private void ScheduleSnapshotRefresh()
        {
            if (_snapshotRefreshScheduled || _restartInProgress)
            {
                return;
            }

            Log.WriteInfoAsync(nameof(ScheduleSnapshotRefresh), string.Empty, $"Order book snapshot refresh scheduled on {DateTime.UtcNow.Add(_snapshotRefreshPeriod)}").GetAwaiter().GetResult();
            _snapshotRefreshScheduled = true;
            _snapshotRefreshTimer.Change(_snapshotRefreshPeriod, Timeout.InfiniteTimeSpan);
        }

        private void CancelSnapshotRefresh()
        {
            if (_snapshotRefreshScheduled)
            {
                Log.WriteInfoAsync(nameof(CancelSnapshotRefresh), string.Empty, "Order book snapshot refresh canceled").GetAwaiter().GetResult();
            }
            _snapshotRefreshScheduled = false;
            _snapshotRefreshTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private bool NeedThrottle()
        {
            var result = false;
            if (MaxOrderBookRate == 0)
            {
                return result;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~OrderBooksHarvesterBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                Stop();
                _messageLoopTask?.Dispose();
                _heartBeatMonitoringTimer?.Dispose();
                _snapshotRefreshTimer.Dispose();
                _measureTask?.Dispose();
            }
        }

        private void SwallowException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.WriteInfoAsync("Stopping", ex.Message, $"Exception was thrown while stopping. Ignore it. {ex}").GetAwaiter().GetResult();
            }
        }
    }
}
