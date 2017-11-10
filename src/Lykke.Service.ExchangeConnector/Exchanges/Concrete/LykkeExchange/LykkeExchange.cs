using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.LykkeExchange.Entities;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Wamp;
using TradingBot.Repositories;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.LykkeExchange
{
    internal class LykkeExchange : Exchange
    {
        public new static readonly string Name = "lykke";
        private static readonly Object _sync = new Object();
        private new LykkeExchangeConfiguration Config => (LykkeExchangeConfiguration) base.Config;
        private readonly ApiClient apiClient;
        private CancellationTokenSource ctSource;
        private Task getPricesTask;
        private WampSubscriber<Candle> wampSubscriber = null;
        private AppSettings appConfig;

        private readonly LinkedList<Guid> ordersToCheckExecution = new LinkedList<Guid>();

        public LykkeExchange(AppSettings appConfig, LykkeExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, ILog log)
            : base(Name, config, translatedSignalsRepository, log)
        {
            this.appConfig = appConfig;
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", Config.ApiKey);
            apiClient = new ApiClient(httpClient, log);
        }


        public async Task<IEnumerable<Instrument>> GetAvailableInstruments(CancellationToken cancellationToken)
        {
            var assetPairs = await apiClient.MakeGetRequestAsync<IEnumerable<AssetPair>>($"{Config.EndpointUrl}/api/AssetPairs", cancellationToken);

            return assetPairs.Select(x => new Instrument(Name, x.Name));
        }

        protected override void StartImpl()
        {
            LykkeLog.WriteInfoAsync(nameof(LykkeExchange), nameof(StartImpl), string.Empty, $"Starting {Name} exchange").Wait();

            if (getPricesTask != null && getPricesTask.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException("The process for getting prices is running already");
            }

            ctSource = new CancellationTokenSource();

            getPricesTask = CheckExecutedOrdersCycle();

            StartWampConnection();
        }

        private void StartWampConnection()
        {
            var wampSettings = new WampSubscriberSettings()
            {
                Address = appConfig.WampEndpoint.Url,
                Realm = appConfig.WampEndpoint.PricesRealm,
                Topics = Instruments.SelectMany(i => new string[] {
                    String.Format(appConfig.WampEndpoint.PricesTopic, i.Name.ToLowerInvariant(), "ask", "sec"),
                    String.Format(appConfig.WampEndpoint.PricesTopic, i.Name.ToLowerInvariant(), "bid", "sec") }).ToArray()
            };

            this.wampSubscriber = new WampSubscriber<Candle>(wampSettings, this.LykkeLog)
                .Subscribe(HandlePrice);

            this.wampSubscriber.Start();
        }

        /// <summary>
        /// Called on incoming prices
        /// </summary>
        /// <param name="candle">Incoming candlestick</param>
        /// <remarks>Can be called simultaneously from multiple threads</remarks>
        private async Task HandlePrice(Candle candle)
        {
            var instrument = Instruments.FirstOrDefault(i => string.Equals(i.Name, candle.Asset, StringComparison.InvariantCultureIgnoreCase));
            if (instrument != null)
            {
                var tickPrice = new TickPrice(
                    instrument,
                    candle.Timestamp,
                    ask: candle.L,
                    bid: candle.H);

                await CallTickPricesHandlers(tickPrice);
            }
        }

        private async Task CheckExecutedOrdersCycle()
        {
            OnConnected();
            while (!ctSource.IsCancellationRequested)
            {
                try
                {
                    await CheckExecutedOrders();
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (Exception e)
                {
                    await LykkeLog.WriteErrorAsync(
                        nameof(LykkeExchange),
                        nameof(LykkeExchange),
                        nameof(CheckExecutedOrdersCycle),
                        e);
                }
            }
            OnStopped();
        }

        protected override void StopImpl()
        {
            this.wampSubscriber?.Stop();
            ctSource?.Cancel();
        }

        protected override async Task<bool> AddOrderImpl(TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            switch (signal.OrderType)
            {
                case OrderType.Market:

                    var marketOrderResponse = await apiClient.MakePostRequestAsync<MarketOrderResponse>(
                        $"{Config.EndpointUrl}/api/Orders/market",
                        CreateHttpContent(new MarketOrderRequest()
                        {
                            AssetPairId = signal.Instrument.Name,
                            OrderAction = signal.TradeType,
                            Volume = signal.Volume
                        }),
                        translatedSignal,
                        CancellationToken.None);

                    return marketOrderResponse != null && marketOrderResponse.Error == null;

                case OrderType.Limit:

                    var limitOrderResponse = await apiClient.MakePostRequestAsync<string>(
                        $"{Config.EndpointUrl}/api/Orders/limit",
                        CreateHttpContent(new LimitOrderRequest()
                        {
                            AssetPairId = signal.Instrument.Name,
                            OrderAction = signal.TradeType,
                            Volume = signal.Volume,
                            Price = signal.Price ?? 0
                        }),
                        translatedSignal,
                        CancellationToken.None);

                    var orderPlaced = limitOrderResponse != null && Guid.TryParse(limitOrderResponse, out var orderId);

                    if (orderPlaced)
                    {
                        translatedSignal.ExternalId = orderId.ToString();
                        ordersToCheckExecution.AddLast(orderId);
                    }

                    return orderPlaced;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private StringContent CreateHttpContent(object value)
        {
            var content = new StringContent(JsonConvert.SerializeObject(value));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }

        protected override async Task<bool> CancelOrderImpl(TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            await apiClient.MakePostRequestAsync<string>(
               $"{Config.EndpointUrl}/api/Orders/{signal.OrderId}/Cancel",
               CreateHttpContent(new object()),
               trasnlatedSignal,
               CancellationToken.None);

            if (Guid.TryParse(signal.OrderId, out var id))
                ordersToCheckExecution.Remove(id);

            return true;
        }

        private async Task CheckExecutedOrders()
        {
            var executedTrades = new List<ExecutedTrade>();

            foreach (var id in ordersToCheckExecution.ToList())
            {
                LimitOrderState state = await GetOrderState(id);

                if (state.Status == LimitOrderStatus.Matched)
                {
                    executedTrades.Add(new ExecutedTrade(new Instrument(Name, state.AssetPairId),
                        DateTime.UtcNow, state.Price, state.Volume, TradeType.Unknown, id.ToString(), ExecutionStatus.Fill));
                    ordersToCheckExecution.Remove(id);
                }
            }

            foreach (var executedTrade in executedTrades)
            {
                await CallExecutedTradeHandlers(executedTrade);
            }
        }

        private Task<LimitOrderState> GetOrderState(Guid externalId)
        {
            return apiClient.MakeGetRequestAsync<LimitOrderState>(
                $"{Config.EndpointUrl}/api/Orders/{externalId}",
                CancellationToken.None);
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout)
        {
            if (await AddOrder(signal, translatedSignal))
            {
                return new ExecutedTrade(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.New);
            }
            else
            {
                return new ExecutedTrade(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.Rejected);
            }
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            if (await CancelOrder(signal, translatedSignal))
            {
                return new ExecutedTrade(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.Cancelled);
            }
            else
            {
                return new ExecutedTrade(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.Rejected);
            }
        }

        public async Task CancelAllOrders()
        {
            var allOrdres = await apiClient.MakeGetRequestAsync<IEnumerable<LimitOrderState>>(
                    $"{Config.EndpointUrl}/api/Orders?status=InOrderBook",
                    CancellationToken.None);


            foreach (var order in allOrdres)
            {
                try
                {
                    await apiClient.MakePostRequestAsync<string>(
                        $"{Config.EndpointUrl}/api/Orders/{order.Id}/Cancel",
                        CreateHttpContent(new object()),
                        null,
                        CancellationToken.None);
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
