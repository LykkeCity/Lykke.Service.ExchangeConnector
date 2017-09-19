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
using TradingBot.Repositories;
using TradingBot.Trading;
using OrderBook = TradingBot.Exchanges.Concrete.LykkeExchange.Entities.OrderBook;

namespace TradingBot.Exchanges.Concrete.LykkeExchange
{
    public class LykkeExchange : Exchange
    {
        public new static readonly string Name = "lykke";
        private new LykkeExchangeConfiguration Config => (LykkeExchangeConfiguration) base.Config;
        private readonly ApiClient apiClient;
        
        public LykkeExchange(LykkeExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, ILog log) 
            : base(Name, config, translatedSignalsRepository, log)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("api-key", Config.ApiKey);
            apiClient = new ApiClient(httpClient, log);
        }

        public async Task<IEnumerable<Instrument>> GetAvailableInstruments(CancellationToken cancellationToken)
        {
            var assetPairs = await apiClient.MakeGetRequestAsync<IEnumerable<AssetPair>>($"{Config.EndpointUrl}/api/AssetPairs", cancellationToken);

            return assetPairs.Select(x => new Instrument(Name, x.Name));
        }


        private CancellationTokenSource ctSource;
        private Task getPricesTask;
        
        protected override void StartImpl()
        {
            if (getPricesTask != null && getPricesTask.Status == TaskStatus.Running)
            {
                throw new InvalidOperationException("The process for getting prices is running already");
            }
            
            ctSource = new CancellationTokenSource();

            getPricesTask = GetPricesCycle();
        }

        private async Task GetPricesCycle()
        {
            OnConnected();
            while (!ctSource.IsCancellationRequested)
            {
                try
                {
                    foreach (var instrument in Instruments)
                    {
                        var orderBook = await apiClient.MakeGetRequestAsync<List<OrderBook>>($"{Config.EndpointUrl}/api/OrderBooks/{instrument.Name}", ctSource.Token);
                        var tickPrices = orderBook.GroupBy(x => x.AssetPair)
                            .Select(g => new InstrumentTickPrices(
                                new Instrument(Name, g.Key),
                                new[]
                                {
                                    new TickPrice(g.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                                        g.FirstOrDefault(ob => !ob.IsBuy)?.Prices.Select(x => x.Price).DefaultIfEmpty(0).Min() ?? 0,
                                        g.FirstOrDefault(ob => ob.IsBuy)?.Prices.Select(x => x.Price).DefaultIfEmpty(0).Max() ?? 0)
                                }))
                            .Where(x => x.TickPrices.First().Ask > 0 && x.TickPrices.First().Bid > 0);

                        foreach (var tickPrice in tickPrices)
                        {
                            await CallHandlers(tickPrice);    
                        }    
                    }

                    await CheckExecutedOrders();
                
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                catch (Exception e)
                {
                    await LykkeLog.WriteErrorAsync(
                        nameof(LykkeExchange),
                        nameof(LykkeExchange),
                        nameof(GetPricesCycle), 
                        e);
                }
            }
            OnStopped();
        }

        protected override void StopImpl()
        {
            ctSource?.Cancel();
        }
        
        
        //private readonly Dictionary<string, Tuple<TradingSignal, TranslatedSignalTableEntity, ExecutedTrade>>

        protected override async Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            switch (signal.OrderType)
            {
                case OrderType.Market:
                    
                    var executionPrice = await apiClient.MakePostRequestAsync<string>(
                        $"{Config.EndpointUrl}/api/Orders/market", 
                        CreateHttpContent(new MarketOrderRequest()
                        {
                            AssetPairId = instrument.Name,
                            OrderAction = signal.TradeType,
                            Volume = signal.Volume
                        }),
                        trasnlatedSignal, 
                        CancellationToken.None);

                    var orderExecuted = decimal.TryParse(executionPrice, out decimal price) && price != default(decimal);

                    return orderExecuted;
                    
                case OrderType.Limit:
                    
                    var orderId = await apiClient.MakePostRequestAsync<string>(
                        $"{Config.EndpointUrl}/api/Orders/limit", 
                        CreateHttpContent(new LimitOrder()
                        {
                            AssetPairId = instrument.Name,
                            OrderAction = signal.TradeType,
                            Volume = signal.Volume,
                            Price = signal.Price
                        }),
                        trasnlatedSignal, 
                        CancellationToken.None);

                    var orderPlaced = Guid.TryParse(orderId, out Guid id) && id != default(Guid);

                    trasnlatedSignal.ExternalId = orderId;
                
                    if (orderPlaced)
                    {
                        lock (orderIds)
                        {
                            orderIds.Add(signal.OrderId, id);
                        }
                    }

                    return orderPlaced;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private readonly Dictionary<string, Guid> orderIds = new Dictionary<string, Guid>();
        
        private StringContent CreateHttpContent(object value)
        {
            var content = new StringContent(JsonConvert.SerializeObject(value));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            //content.Headers.Add("api-key", Config.ApiKey);
            
            return content;
        }

        protected override async Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal)
        {
            Guid orderId;
            lock (orderIds)
            {
                if (orderIds.ContainsKey(signal.OrderId))
                {
                    orderId = orderIds[signal.OrderId];
                }
            }
            
             await apiClient.MakePostRequestAsync<string>(
                $"{Config.EndpointUrl}/api/Orders/{orderId}/Cancel", 
                CreateHttpContent(new object()),
                trasnlatedSignal, 
                CancellationToken.None);

            return true;
        }

        private async Task CheckExecutedOrders()
        {
            var executedTrades = new List<ExecutedTrade>();

            foreach (var pair in ActualSignals)
            {
                foreach (var signal in pair.Value.ToList())
                {
                    Guid orderId;
                    lock (orderIds)
                    {
                        if (orderIds.ContainsKey(signal.OrderId))
                        {
                            orderId = orderIds[signal.OrderId];
                        }
                    }

                    if (await CheckIsOrderExecuted(orderId))
                    {
                        executedTrades.Add(new ExecutedTrade(new Instrument(Name, pair.Key), DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType, signal.OrderId, ExecutionStatus.Fill));
                    }    
                }
            }

            foreach (var executedTrade in executedTrades)
            {
                await CallExecutedTradeHandlers(executedTrade);    
            }
        }

        private async Task<bool> CheckIsOrderExecuted(Guid externalId)
        {
            try
            {
                LimitOrderState state = await apiClient.MakeGetRequestAsync<LimitOrderState>(
                    $"{Config.EndpointUrl}/api/Orders/{externalId}",
                    CancellationToken.None);

                return state.Status == LimitOrderStatus.Matched;
            }
            catch (Exception e)
            {
                await LykkeLog.WriteErrorAsync(
                    nameof(LykkeExchange),
                    nameof(LykkeExchange),
                    nameof(CheckIsOrderExecuted), 
                    e);
                
                return false;
            }
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout)
        {
            if (await AddOrder(instrument, signal, translatedSignal))
            {
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.New);
            }
            else
            {
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.Rejected);
            }
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            if (await CancelOrder(instrument, signal, translatedSignal))
            {
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType,
                    signal.OrderId, ExecutionStatus.Cancelled);
            }
            else
            {
                return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType,
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