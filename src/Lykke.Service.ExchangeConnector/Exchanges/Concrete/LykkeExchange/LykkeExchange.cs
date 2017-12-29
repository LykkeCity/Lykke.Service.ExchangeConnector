using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.LykkeExchange.Entities;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Wamp;
using TradingBot.Repositories;
using TradingBot.Trading;
using AssetPair = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.AssetPair;
using OrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;
using OrderType = TradingBot.Trading.OrderType;

namespace TradingBot.Exchanges.Concrete.LykkeExchange
{
    internal class LykkeExchange : Exchange
    {
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<OrderStatusUpdate> _tradeHandler;
        public new static readonly string Name = "lykke";
        private new LykkeExchangeConfiguration Config => (LykkeExchangeConfiguration) base.Config;
        private readonly ApiClient apiClient;
        private CancellationTokenSource ctSource;
        private WampSubscriber<Candle> wampSubscriber = null;
        private RabbitMqSubscriber<OrderBook> orderbooksRabbit;
        private RabbitMqSubscriber<LimitOrderMessage> orderStatusesRabbit;

        private readonly Dictionary<string, decimal> _lastBids;
        private readonly Dictionary<string, decimal> _lastAsks;

        public LykkeExchange(LykkeExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, IHandler<TickPrice> tickPriceHandler, IHandler<OrderStatusUpdate> tradeHandler, ILog log)
            : base(Name, config, translatedSignalsRepository, log)
        {
            _tickPriceHandler = tickPriceHandler;
            _tradeHandler = tradeHandler;
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", Config.ApiKey);
            apiClient = new ApiClient(httpClient, log);

            _lastBids = Instruments.ToDictionary(x => x.Name, x => 0m);
            _lastAsks = Instruments.ToDictionary(x => x.Name, x => 0m);
        }


        public async Task<IEnumerable<Instrument>> GetAvailableInstruments(CancellationToken cancellationToken)
        {
            var assetPairs = await apiClient.MakeGetRequestAsync<IEnumerable<AssetPair>>($"{Config.EndpointUrl}/api/AssetPairs", cancellationToken);

            return assetPairs.Select(x => new Instrument(Name, x.Name));
        }

        protected override void StartImpl()
        {
            LykkeLog.WriteInfoAsync(nameof(LykkeExchange), nameof(StartImpl), string.Empty, $"Starting {Name} exchange").Wait();

            ctSource = new CancellationTokenSource();

            //StartWampConnection(); // TODO: wamp sends strange tickprices with ask=bid, temporary switch to direct rabbitmq connection:

            GetInitialTickPrices().Wait();
            StartRabbitMqTickPriceSubscription();
            StartRabbitMqOrdersSubscription();
            OnConnected();
        }

        private async Task GetInitialTickPrices()
        {
            foreach (var instrument in Instruments)
            {
                try
                {
                    var orderBook =
                        await apiClient.MakeGetRequestAsync<List<OrderBook>>(
                            $"{Config.EndpointUrl}/api/OrderBooks/{instrument.Name}", ctSource.Token);
                    var tickPrices = orderBook.GroupBy(x => x.AssetPair)
                        .Select(g => new TickPrice(
                            new Instrument(Name, g.Key),
                            g.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                            g.FirstOrDefault(ob => !ob.IsBuy)?.Prices.Select(x => x.Price).DefaultIfEmpty(0).Min() ?? 0,
                            g.FirstOrDefault(ob => ob.IsBuy)?.Prices.Select(x => x.Price).DefaultIfEmpty(0).Max() ?? 0)
                        )
                        .Where(x => x.Ask > 0 && x.Bid > 0);

                    foreach (var tickPrice in tickPrices)
                    {
                        _lastAsks[instrument.Name] = tickPrice.Ask;
                        _lastBids[instrument.Name] = tickPrice.Bid;
                        
                        await _tickPriceHandler.Handle(tickPrice);
                    }
                }
                catch (Exception e)
                {
                    await LykkeLog.WriteErrorAsync(nameof(LykkeExchange), nameof(GetInitialTickPrices), e);
                }
            }
        }

        private void StartRabbitMqTickPriceSubscription()
        {
            var rabbitSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = Config.RabbitMq.OrderBook.ConnectionString,
                ExchangeName = Config.RabbitMq.OrderBook.Exchange,
                QueueName = Config.RabbitMq.OrderBook.Queue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(LykkeLog, rabbitSettings);
            orderbooksRabbit = new RabbitMqSubscriber<OrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<OrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(LykkeLog)
                .Subscribe(HandleOrderBook)
                .Start();
        }

        
        private async Task HandleOrderBook(OrderBook orderBook)
        {
            var instrument = Instruments.SingleOrDefault(x => 
                string.Compare(x.Name, orderBook.AssetPair, StringComparison.InvariantCultureIgnoreCase) == 0);
            if (instrument != null)
            {
                if (orderBook.Prices.Any())
                {
                    decimal bestBid = 0;
                    decimal bestAsk = 0;

                    if (orderBook.IsBuy)
                    {
                        _lastBids[instrument.Name] = bestBid = orderBook.Prices.Select(x => x.Price).OrderByDescending(x => x).First();
                        bestAsk = _lastAsks[instrument.Name];
                    }
                    else
                    {
                        _lastAsks[instrument.Name] = bestAsk = orderBook.Prices.Select(x => x.Price).OrderBy(x => x).First();
                        bestBid = _lastBids[instrument.Name];
                    }
                    
                    if (bestBid > 0 && bestAsk > 0)
                    {
                        var tickPrice = new TickPrice(instrument, orderBook.Timestamp, bestAsk, bestBid);
                        await _tickPriceHandler.Handle(tickPrice);
                    }
                }
            }
        }

        private void StartRabbitMqOrdersSubscription()
        {
            var rabbitSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = Config.RabbitMq.OrderBook.ConnectionString,
                ExchangeName = Config.RabbitMq.Orders.Exchange,
                QueueName = Config.RabbitMq.Orders.Queue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(LykkeLog, rabbitSettings);
            orderStatusesRabbit = new RabbitMqSubscriber<LimitOrderMessage>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<LimitOrderMessage>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(LykkeLog)
                .Subscribe(HandleOrderStatus)
                .Start();
        }

        private async Task HandleOrderStatus(LimitOrderMessage message)
        {
            foreach (var order in message.Orders.Where(x => x.Order.ClientId == Config.ClientId))
            {
                if (order.Order.Status == OrderStatus.Cancelled)
                {
                    await LykkeLog.WriteInfoAsync(nameof(LykkeExchange), nameof(HandleOrderStatus), order.ToString(),
                        "Order canceled. Calling ExecutedTradeHandlers");
                    
                    await _tradeHandler.Handle(new OrderStatusUpdate(new Instrument(Name, order.Order.AssetPairId),
                        DateTime.UtcNow,
                        order.Order.Price ?? 0, 
                        Math.Abs(order.Order.Volume), 
                        order.Order.Volume < 0 ? TradeType.Sell : TradeType.Buy, 
                        order.Order.ExternalId,
                        OrderExecutionStatus.Cancelled));
                }
                else if (order.Order.Status == OrderStatus.Matched && order.Trades.Any())
                {
                    await LykkeLog.WriteInfoAsync(nameof(LykkeExchange), nameof(HandleOrderStatus), order.ToString(),
                        "Order executed. Calling ExecutedTradeHandlers");
                    
                    await _tradeHandler.Handle(new OrderStatusUpdate(new Instrument(Name, order.Order.AssetPairId),
                        order.Trades.Last().Timestamp,
                        order.Order.Price ?? order.Trades.Last().Price ?? 0,
                        Math.Abs(order.Order.Volume - order.Order.RemainingVolume),
                        order.Order.Volume < 0 ? TradeType.Sell : TradeType.Buy, 
                        order.Order.ExternalId,
                        OrderExecutionStatus.Fill));
                }
            }
        }

        // TODO: wamp sends strange tickprices with ask=bid, temporary switch to direct rabbitmq connection
//        private void StartWampConnection()
//        {
//            var wampSettings = new WampSubscriberSettings()
//            {
//                Address = Config.WampEndpoint.Url,
//                Realm = Config.WampEndpoint.PricesRealm,
//                Topics = Instruments.SelectMany(i => new string[] {
//                    String.Format(Config.WampEndpoint.PricesTopic, i.Name.ToLowerInvariant(), "ask", "sec"),
//                    String.Format(Config.WampEndpoint.PricesTopic, i.Name.ToLowerInvariant(), "bid", "sec") }).ToArray()
//            };
//
//            this.wampSubscriber = new WampSubscriber<Candle>(wampSettings, this.LykkeLog)
//                .Subscribe(HandlePrice);
//
//            this.wampSubscriber.Start();
//        }

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

                await _tickPriceHandler.Handle(tickPrice);
            }
        }

        protected override void StopImpl()
        {
            this.wampSubscriber?.Stop();
            ctSource?.Cancel();
            OnStopped();
        }

        private StringContent CreateHttpContent(object value)
        {
            var content = new StringContent(JsonConvert.SerializeObject(value));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return content;
        }

        public override async Task<OrderStatusUpdate> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            
            
            switch (signal.OrderType)
            {
                case OrderType.Market:

                    var marketOrderResponse = await apiClient.MakePostRequestAsync<MarketOrderResponse>(
                        $"{Config.EndpointUrl}/api/Orders/market",
                        CreateHttpContent(new MarketOrderRequest()
                        {
                            AssetPairId = signal.Instrument.Name,
                            Asset = signal.Instrument.Base,
                            OrderAction = signal.TradeType,
                            Volume = signal.Volume
                        }),
                        translatedSignal.RequestSent, translatedSignal.ResponseReceived,
                        cts.Token);

                    if (marketOrderResponse != null && marketOrderResponse.Error == null)
                    {
                        return new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, marketOrderResponse.Result,
                            signal.Volume, signal.TradeType,
                            signal.OrderId, OrderExecutionStatus.Fill);
                    }
                    else
                    {
                        throw new ApiException("Unexpected result from exchange");
                    }

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
                        translatedSignal.RequestSent, translatedSignal.ResponseReceived,
                        cts.Token);

                    var orderPlaced = limitOrderResponse != null && Guid.TryParse(limitOrderResponse, out var orderId);

                    if (orderPlaced)
                    {
                        translatedSignal.ExternalId = orderId.ToString();
                        return new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume,
                            signal.TradeType,
                            orderId.ToString(), OrderExecutionStatus.New);
                    }
                    else
                    {
                        throw new ApiException("Unexpected result from exchange");
                    }

                default:
                    throw new ApiException($"Unsupported OrderType {signal.OrderType}");
            }
        }

        public override async Task<OrderStatusUpdate> CancelOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            
            string result = await apiClient.MakePostRequestAsync<string>(
                $"{Config.EndpointUrl}/api/Orders/{signal.OrderId}/Cancel",
                CreateHttpContent(new object()),
                translatedSignal.RequestSent, translatedSignal.ResponseReceived,
                cts.Token);
            
            return new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType,
                signal.OrderId, OrderExecutionStatus.Cancelled);
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
                        null, null,
                        CancellationToken.None);
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
