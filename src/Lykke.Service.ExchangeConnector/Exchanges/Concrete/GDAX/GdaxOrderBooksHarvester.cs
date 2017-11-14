using System;
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

        public GdaxOrderBooksHarvester(GdaxExchangeConfiguration configuration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(configuration, log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
            _websocketApi = CreateWebSocketsApiClient();
            _restApi = CreateRestApiClient();
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await _websocketApi.ConnectAsync(CancellationToken);
                // TODO: First subscribe with websockets and ignore the events before the GetOpenOrders execution
                await HandleOpenedOrders(await _restApi.GetOpenOrders(CancellationToken)); // Send symbol 

                await _websocketApi.SubscribeToFullUpdatesAsync(
                    _configuration.Instruments.Select(ConvertSymbolFromLykkeToExchange).ToArray(),
                    CancellationToken);
            }
            finally
            {
                try
                {
                    using (var cts = new CancellationTokenSource(5000))
                    {
                        await _websocketApi.CloseConnectionAsync(cts.Token);
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
                _configuration.PassPhrase)
            {
                BaseUri = new Uri(_configuration.RestEndpointUrl),
                ConnectorUserAgent = _configuration.UserAgent
            };
        }

        private GdaxWebSocketApi CreateWebSocketsApiClient()
        {
            var websocketApi = new GdaxWebSocketApi(_configuration.ApiKey, _configuration.ApiSecret,
                _configuration.PassPhrase)
            {
                BaseUri = new Uri(_configuration.WssEndpointUrl)
            };
            websocketApi.Ticker += OnWebSocketTicker;
            websocketApi.OrderReceived += OnWebSocketOrderReceived;
            websocketApi.OrderChanged += OnOrderChanged;
            websocketApi.OrderDone += OnWebSocketOrderDone;

            return websocketApi;
        }

        private async Task HandleOpenedOrders(IReadOnlyList<GdaxOrderResponse> orders)
        {
            var groupedOrders = from order in orders
                                group order by order.ProductId into gr
                                select gr;

            foreach (var orderGroup in groupedOrders)
            {
                await HandleOrdebookSnapshotAsync(orderGroup.Key,
                    DateTime.UtcNow,
                    orderGroup.Select(order =>
                        new OrderBookItem
                        {
                            Id = order.Id.ToString(),
                            IsBuy = order.Side == GdaxOrderSide.Buy,
                            Symbol = order.ProductId,
                            Price = order.Price,
                            Size = order.Size
                        }));
            }
        }

        private void OnWebSocketTicker(object sender, GdaxWssTicker ticker)
        {
            // TODO Handle order book changes for sanity check
        }

        private async void OnWebSocketOrderReceived(object sender, GdaxWssOrderReceived order)
        {
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

        private async void OnOrderChanged(object sender, GdaxWssOrderChange order)
        {
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

        private async void OnWebSocketOrderDone(object sender, GdaxWssOrderDone order)
        {
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
    }
}
