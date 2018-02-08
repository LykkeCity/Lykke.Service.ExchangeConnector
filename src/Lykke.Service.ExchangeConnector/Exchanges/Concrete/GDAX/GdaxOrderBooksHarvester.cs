using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.WssClient.Entities;
using Lykke.ExternalExchangesApi.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.GDAX.Entities;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal sealed class GdaxOrderBooksHarvester : OrderBooksHarvesterBase
    {
        private readonly GdaxExchangeConfiguration _configuration;
        private readonly GdaxWebSocketApi _websocketApi;
        private readonly GdaxRestApi _restApi;
        private readonly ConcurrentDictionary<string, long> _symbolsLastSequenceNumbers;
        private readonly IDictionary<string, Queue<GdaxQueueOrderItem>> _queuedOrderBookItems;
        private readonly GdaxConverters _converters;

        public GdaxOrderBooksHarvester(GdaxExchangeConfiguration configuration, ILog log,
            IHandler<OrderBook> orderBookHandler)
            : base(GdaxExchange.Name, configuration, log, orderBookHandler)
        {
            _configuration = configuration;
            _symbolsLastSequenceNumbers = new ConcurrentDictionary<string, long>();
            _queuedOrderBookItems = new Dictionary<string, Queue<GdaxQueueOrderItem>>();

            _websocketApi = CreateWebSocketsApiClient();
            _restApi = CreateRestApiClient();
            _converters = new GdaxConverters(_configuration.SupportedCurrencySymbols,
                ExchangeName);
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await _websocketApi.ConnectAsync(CancellationToken);

                // First subscribe with websockets and ignore all the order events with 
                // sequential number less than symbol's orderbook orders
                var subscriptionTask = new AsyncEventAwaiter<string>(ref _websocketApi.Subscribed)
                    .Await(CancellationToken);
                var ordersListenerTask = _websocketApi.SubscribeToOrderBookUpdatesAsync(
                    _configuration.SupportedCurrencySymbols.Select(s => s.ExchangeSymbol).ToList(),
                    CancellationToken);

                await subscriptionTask; // Wait to be subscribed first
                var retrieveOrderBooksTask = Task.Run(async () =>
                {
                    foreach (var currencySymbol in _configuration.SupportedCurrencySymbols)
                    {
                        var orderBook = await _restApi.GetFullOrderBook(
                            currencySymbol.ExchangeSymbol, CancellationToken);
                        await HandleRetrievedOrderBook(currencySymbol.ExchangeSymbol, orderBook);
                    }
                });

                await Task.WhenAll(ordersListenerTask, retrieveOrderBooksTask);
            }
            finally
            {
                try
                {
                    if (_websocketApi != null)
                    {
                        using (var cts = new CancellationTokenSource(5000))
                        {
                            await _websocketApi.CloseConnectionAsync(cts.Token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(GdaxOrderBooksHarvester),
                        "Could not close web sockets connection properly", ex);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _websocketApi?.Dispose();
                _restApi?.Dispose();
            }
        }

        private GdaxRestApi CreateRestApiClient()
        {
            return new GdaxRestApi(_configuration.ApiKey, _configuration.ApiSecret,
                _configuration.PassPhrase, _configuration.RestEndpointUrl, _configuration.UserAgent);
        }

        private GdaxWebSocketApi CreateWebSocketsApiClient()
        {
            var websocketApi = new GdaxWebSocketApi(new LogToConsole(),
                _configuration.ApiKey, _configuration.ApiSecret, _configuration.PassPhrase,
                _configuration.WssEndpointUrl);
            websocketApi.Ticker += OnWebSocketTickerAsync;
            websocketApi.OrderReceived += OnWebSocketOrderReceivedAsync;
            websocketApi.OrderChanged += OnOrderChangedAsync;
            websocketApi.OrderDone += OnWebSocketOrderDoneAsync;

            return websocketApi;
        }

        private async Task HandleRetrievedOrderBook(string symbol, GdaxOrderBook orderBook)
        {
            var orders = orderBook.Asks.Select(order =>
                    _converters.GdaxOrderBookItemToOrderBookItem(symbol, false, order))
                .Union(orderBook.Bids.Select(order =>
                    _converters.GdaxOrderBookItemToOrderBookItem(symbol, true, order)));

            await HandleOrderBookSnapshotAsync(symbol, DateTime.UtcNow, orders);

            _symbolsLastSequenceNumbers[symbol] = orderBook.Sequence;
        }

        private Task OnWebSocketTickerAsync(object sender, GdaxWssTicker ticker)
        {
            // TODO Handle order book changes for sanity check
            return Task.FromResult(0);
        }

        private async Task OnWebSocketOrderReceivedAsync(object sender, GdaxWssOrderReceived order)
        {
            await HandleOrderEventAsync(order.ProductId, order.Sequence, OrderBookEventType.Add,
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.Size
                });
        }

        private async Task OnOrderChangedAsync(object sender, GdaxWssOrderChange order)
        {
            await HandleOrderEventAsync(order.ProductId, order.Sequence, OrderBookEventType.Update,
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.NewSize
                });
        }

        private async Task OnWebSocketOrderDoneAsync(object sender, GdaxWssOrderDone order)
        {
            await HandleOrderEventAsync(order.ProductId, order.Sequence, OrderBookEventType.Delete,
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.RemainingSize
                    // TODO Handle reason: order.Reason == "cancelled" ? ExecutionStatus.Cancelled : ExecutionStatus.Fill
                });
        }

        private async Task HandleOrderEventAsync(string productId, long orderEventSequenceNumber,
            OrderBookEventType eventType, OrderBookItem orderBookItem)
        {
            // Queue this item if the order book is not fully populated yet
            var orderBookExists =
                _symbolsLastSequenceNumbers.TryGetValue(productId, out long seqNumberInOrderBook) &&
                TryGetOrderBookSnapshot(productId, out var _);

            if (!orderBookExists)
            {
                QueueItem(productId, new GdaxQueueOrderItem(orderEventSequenceNumber,
                    eventType, orderBookItem));
                return;
            }

            // Dequeue all items in the order events queue
            foreach (var queuedItem in DequeuOrderItems(productId)
                .Where(q => q.SequenceNumber > seqNumberInOrderBook))
            {
                await HandleOrdersEventsAsync(productId, queuedItem.OrderBookEventType,
                    new[] { queuedItem.OrderBookItem });
            }

            // Handle the current item
            if (orderEventSequenceNumber > seqNumberInOrderBook)
                await HandleOrdersEventsAsync(productId, eventType, new[] { orderBookItem });
        }

        private void QueueItem(string productId, GdaxQueueOrderItem queueOrderItem)
        {
            var queueExist = _queuedOrderBookItems.TryGetValue(productId, out var productOrdersQueue);
            if (!queueExist)
            {
                productOrdersQueue = new Queue<GdaxQueueOrderItem>();
                _queuedOrderBookItems[productId] = productOrdersQueue;
            }

            productOrdersQueue.Enqueue(queueOrderItem);
        }

        private IEnumerable<GdaxQueueOrderItem> DequeuOrderItems(string productId)
        {
            if (_queuedOrderBookItems.TryGetValue(productId, out var productOrdersQueue))
                while (productOrdersQueue.Count > 0)
                    yield return productOrdersQueue.Dequeue();
        }
    }
}
