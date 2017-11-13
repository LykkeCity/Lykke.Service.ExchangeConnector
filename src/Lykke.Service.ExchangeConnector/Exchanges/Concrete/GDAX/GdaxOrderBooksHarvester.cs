using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
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

        public GdaxOrderBooksHarvester(GdaxExchangeConfiguration configuration, ILog log,
            OrderBookSnapshotsRepository orderBookSnapshotsRepository, OrderBookEventsRepository orderBookEventsRepository)
            : base(configuration, log, orderBookSnapshotsRepository, orderBookEventsRepository)
        {
            _configuration = configuration;
            _websocketApi = CreateWebSocketsApiClient();
        }

        protected override async Task MessageLoopImpl()
        {
            try
            {
                await _websocketApi.ConnectAsync(CancellationToken);
                await _websocketApi.SubscribeToFullUpdatesAsync(
                    _configuration.Instruments.Select(ConvertSymbolFromLykkeToExchange).ToArray(),
                    CancellationToken);
            }
            finally
            {
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        var operationSucceeded = await _websocketApi
                            .CloseConnectionAsync(cts.Token)
                            .AwaitWithTimeout(5000);
                        if (!operationSucceeded)
                            cts.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    await Log.WriteErrorAsync(nameof(GdaxOrderBooksHarvester), 
                        "Could not close web sockets connection", ex);
                }
            }
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

        private void OnWebSocketTicker(object sender, GdaxWssTicker ticker)
        {
            // TODO
            // await HandleOrdebookSnapshotAsync(table.Attributes.Symbol,
            //DateTime.UtcNow, // TODO: Use server's date
            //orderBookItems);
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
