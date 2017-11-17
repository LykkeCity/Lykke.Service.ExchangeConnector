using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Concrete.GDAX.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;
using TradingBot.Exchanges.Concrete.GDAX.WssClient;
using TradingBot.Exchanges.Concrete.GDAX.WssClient.Entities;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal sealed class GdaxOrderBooksHarvester : OrderBooksHarvesterBase
    {
        private readonly GdaxExchangeConfiguration _configuration;
        private readonly GdaxWebSocketApi _websocketApi;
        private readonly GdaxRestApi _restApi;
        private ConcurrentDictionary<string, long> _symbolsLastSequenceNumbers;
        private readonly GdaxConverters _converters;

        public GdaxOrderBooksHarvester(GdaxExchangeConfiguration configuration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(configuration, log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
            _symbolsLastSequenceNumbers = new ConcurrentDictionary<string, long>();

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
                var subscriptionTask = _websocketApi.SubscribeToFullUpdatesAsync(
                    _configuration.SupportedCurrencySymbols.Select(s => s.ExchangeSymbol).ToList(),
                    CancellationToken);

                var retrieveOrderBooksTask = Task.Run(async () =>
                {
                    foreach (var currencySymbol in _configuration.SupportedCurrencySymbols)
                    {
                        var orderBook = await _restApi.GetFullOrderBook(
                            currencySymbol.ExchangeSymbol, CancellationToken);
                        await HandleRetrievedOrderBook(currencySymbol.ExchangeSymbol, orderBook);
                    }
                });

                await Task.WhenAll(subscriptionTask, retrieveOrderBooksTask);
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

            await HandleOrdebookSnapshotAsync(symbol, DateTime.UtcNow, orders);

            _symbolsLastSequenceNumbers[symbol] = orderBook.Sequence;
        }

        private Task OnWebSocketTickerAsync(object sender, GdaxWssTicker ticker)
        {
            // TODO Handle order book changes for sanity check
            return Task.FromResult(0);
        }

        private async Task OnWebSocketOrderReceivedAsync(object sender, GdaxWssOrderReceived order)
        {
            if (!ShouldProcessOrder(order.ProductId, order.Sequence))
                return;

            await HandleOrdersEventsAsync(order.ProductId, OrderBookEventType.Add, new[]
            {
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.Size
                }
            });
        }

        private async Task OnOrderChangedAsync(object sender, GdaxWssOrderChange order)
        {
            if (!ShouldProcessOrder(order.ProductId, order.Sequence))
                return;

            await HandleOrdersEventsAsync(order.ProductId, OrderBookEventType.Update, new[]
            {
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.NewSize
                }
            });
        }

        private async Task OnWebSocketOrderDoneAsync(object sender, GdaxWssOrderDone order)
        {
            if (!ShouldProcessOrder(order.ProductId, order.Sequence))
                return;

            await HandleOrdersEventsAsync(order.ProductId, OrderBookEventType.Delete, new[]
            {
                new OrderBookItem
                {
                    Id = order.OrderId.ToString(),
                    IsBuy = order.Side == GdaxOrderSide.Buy,
                    Symbol = order.ProductId,
                    Price = order.Price ?? 0,
                    Size = order.RemainingSize
                    // TODO Handle reason: order.Reason == "cancelled" ? ExecutionStatus.Cancelled : ExecutionStatus.Fill
                }
            });
        }

        private bool ShouldProcessOrder(string symbol, long orderSequenceNumber)
        {
            return (_symbolsLastSequenceNumbers.TryGetValue(symbol, out long seqNumberInOrderBook)) &&
                seqNumberInOrderBook < orderSequenceNumber;
        }
    }
}
