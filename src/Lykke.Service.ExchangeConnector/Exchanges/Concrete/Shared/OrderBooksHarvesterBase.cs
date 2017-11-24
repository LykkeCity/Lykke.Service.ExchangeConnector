using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Polly;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Shared
{
    internal abstract class OrderBooksHarvesterBase : IDisposable
    {
        protected const double MeasurePeriodSec = 60;

        protected readonly ILog Log;
        protected readonly OrderBookSnapshotsRepository OrderBookSnapshotsRepository;
        protected readonly OrderBookEventsRepository OrderBookEventsRepository;
        protected CancellationToken CancellationToken;
        protected long ReceivedMessages;
        protected long PublishedToRabbit;

        private readonly ConcurrentDictionary<string, OrderBookSnapshot> _orderBookSnapshots;
        private readonly ExchangeConverters _converters;
        private readonly Timer _heartBeatMonitoringTimer;
        private readonly TimeSpan _heartBeatPeriod = TimeSpan.FromSeconds(30);
        private CancellationTokenSource _cancellationTokenSource;
        private Task _messageLoopTask;
        private Func<OrderBook, Task> _newOrderBookHandler;
        private DateTime _lastPublishTime = DateTime.MinValue;
        private int _orderBooksReceivedInLastTimeFrame;
        private Task _measureTask;

        protected IExchangeConfiguration ExchangeConfiguration { get; }

        public string ExchangeName { get; }

        public int MaxOrderBookRate { get; set; }

        protected OrderBooksHarvesterBase(string exchangeName, IExchangeConfiguration exchangeConfiguration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
        {
            ExchangeConfiguration = exchangeConfiguration;
            OrderBookSnapshotsRepository = orderBookSnapshotsRepository;
            OrderBookEventsRepository = orderBookEventsRepository;
            ExchangeName = exchangeName;

            Log = log.CreateComponentScope(GetType().Name);

            _converters = new ExchangeConverters(exchangeConfiguration.SupportedCurrencySymbols,
                string.Empty);

            _orderBookSnapshots = new ConcurrentDictionary<string, OrderBookSnapshot>();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            _heartBeatMonitoringTimer = new Timer(ForceStopMessenger);
        }

        private async void ForceStopMessenger(object state)
        {
            await Log.WriteWarningAsync(nameof(ForceStopMessenger), "Monitoring heartbeat",
                $"Heart stopped. Restarting {GetType().Name}");
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

        protected virtual async Task Measure()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                var msgInSec = ReceivedMessages / MeasurePeriodSec;
                var pubInSec = PublishedToRabbit / MeasurePeriodSec;
                await Log.WriteInfoAsync(nameof(OrderBooksHarvesterBase),
                    $"Receive rate from {ExchangeName} {msgInSec} per second, publish rate to " +
                    $"RabbitMq {pubInSec} per second", string.Empty);
                ReceivedMessages = 0;
                PublishedToRabbit = 0;
                await Task.Delay(TimeSpan.FromSeconds(MeasurePeriodSec), CancellationToken);
            }
        }

        public void AddHandler(Func<OrderBook, Task> handler)
        {
            _newOrderBookHandler = handler;
        }

        public void Start()
        {
            Log.WriteInfoAsync(nameof(Start), "Starting", $"Starting {GetType().Name}").Wait();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            _measureTask = Task.Run(async () => await Measure());
            StartReading();
        }

        protected virtual void StartReading()
        {
            _messageLoopTask = Task.Run(async () => await MessageLoop());
        }

        public void Stop()
        {
            Log.WriteInfoAsync(nameof(Stop), "Stopping", $"Stopping {GetType().Name}").Wait();
            _cancellationTokenSource?.Cancel();
            SwallowCanceledException(() => 
                _messageLoopTask?.GetAwaiter().GetResult());
            SwallowCanceledException(() => 
                _measureTask?.GetAwaiter().GetResult());
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task MessageLoop()
        {
            const int smallTimeout = 5;
            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryForeverAsync(attempt => attempt % 60 == 0
                    ? TimeSpan.FromMinutes(5)
                    : TimeSpan.FromSeconds(smallTimeout)); // After every 60 attempts wait 5min 

            await retryPolicy.ExecuteAsync(async () =>
             {
                 await Log.WriteInfoAsync(nameof(MessageLoopImpl), "Starting message loop", "");
                 try
                 {
                     await MessageLoopImpl();
                 }
                 catch (Exception ex)
                 {
                     await Log.WriteErrorAsync(nameof(MessageLoopImpl),
                         $"An exception occurred while working with WebSocket. Reconnect in {smallTimeout} sec", ex);
                     throw;
                 }
             });
        }

        protected async Task PublishOrderBookSnapshotAsync()
        {
            ReceivedMessages++;
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
            PublishedToRabbit++;

            foreach (var orderBook in orderBooks)
            {
                await _newOrderBookHandler(orderBook);
            }
        }

        protected abstract Task MessageLoopImpl();

        protected async Task<OrderBookSnapshot> GetOrderBookSnapshot(string pair)
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

        protected async Task HandleOrdebookSnapshotAsync(string pair, DateTime timeStamp, IEnumerable<OrderBookItem> orders)
        {
            var orderBookSnapshot = new OrderBookSnapshot(ExchangeName, pair, timeStamp);
            orderBookSnapshot.AddOrUpdateOrders(orders);
            _orderBookSnapshots[pair] = orderBookSnapshot;

            if (ExchangeConfiguration.SaveOrderBooksToAzure)
                await OrderBookSnapshotsRepository.SaveAsync(orderBookSnapshot);

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

            if (ExchangeConfiguration.SaveOrderBooksToAzure)
            {
                await OrderBookEventsRepository.SaveAsync(new OrderBookEvent
                {
                    SnapshotId = orderBookSnapshot.GeneratedId,
                    EventType = orderEventType,
                    OrderEventTimestamp = DateTime.UtcNow,
                    OrderItems = orders
                });
            }

            await PublishOrderBookSnapshotAsync();
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
                _measureTask?.Dispose();
            }
        }

        private void SwallowCanceledException(Action action)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
