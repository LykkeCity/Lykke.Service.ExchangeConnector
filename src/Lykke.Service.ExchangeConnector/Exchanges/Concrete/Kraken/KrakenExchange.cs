using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
using Lykke.ExternalExchangesApi.Helpers;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Kraken
{
    internal class KrakenExchange : Exchange
    {
        public new static readonly string Name = "kraken";

        private readonly KrakenConfig config;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<ExecutionReport> _tradeHandler;

        private readonly PublicData publicData;
        private readonly PrivateData privateData;

        private Task pricesJob;
        private CancellationTokenSource ctSource;

        public KrakenExchange(KrakenConfig config, TranslatedSignalsRepository translatedSignalsRepository, IHandler<TickPrice> tickPriceHandler, IHandler<ExecutionReport> tradeHandler, ILog log) :
            base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
            _tickPriceHandler = tickPriceHandler;
            _tradeHandler = tradeHandler;

            var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) }; // TODO: HttpClient have to be Singleton
            publicData = new PublicData(new ApiClient(httpClient, log));
            privateData = new PrivateData(new ApiClient(new HttpClient() { Timeout = TimeSpan.FromSeconds(30) }, log), config.ApiKey, config.PrivateKey,
                new NonceProvider(), Config.SupportedCurrencySymbols);
        }

        protected override void StartImpl()
        {
            ctSource = new CancellationTokenSource();

            CheckServerTime(ctSource.Token)
                .ContinueWith(task =>
                {
                    if (!task.IsFaulted && task.Result)
                        OnConnected();
                });

            if (config.PubQuotesToRabbit)
            {
                pricesJob = Task.Run(async () =>
                {
                    var lasts = Instruments.ToDictionary(x => x.Name, x => 0L);

                    while (!ctSource.IsCancellationRequested)
                    {
                        try
                        {
                            foreach (var pair in config.SupportedCurrencySymbols)
                            {
                                SpreadDataResult result;

                                try
                                {
                                    result = await publicData.GetSpread(ctSource.Token, pair.ExchangeSymbol, lasts[pair.LykkeSymbol]);
                                }
                                catch (Exception e)
                                {
                                    await LykkeLog.WriteErrorAsync(
                                        nameof(Kraken),
                                        nameof(KrakenExchange),
                                        nameof(pricesJob),
                                        e);
                                    continue;
                                }

                                lasts[pair.LykkeSymbol] = result.Last;
                                var prices = result.Data.Single().Value.Select(x =>
                                    new TickPrice(Instruments.Single(i => i.Name == pair.LykkeSymbol),
                                    x.Time, x.Ask, x.Bid)).ToArray();

                                if (prices.Any())
                                {
                                    if (prices.Length == 1 && prices[0].Time == DateTimeUtils.FromUnix(lasts[pair.LykkeSymbol]))
                                    {
                                        // If there is only one price and it has timestamp of last one, ignore it.
                                    }
                                    else
                                    {
                                        foreach (var tickPrice in prices)
                                        {
                                            await _tickPriceHandler.Handle(tickPrice);
                                        }
                                    }
                                }

                                await Task.Delay(TimeSpan.FromSeconds(10), ctSource.Token);
                            }

                            await CheckExecutedOrders();
                        }
                        catch (Exception e)
                        {
                            await LykkeLog.WriteErrorAsync(
                                nameof(Kraken),
                                nameof(KrakenExchange),
                                nameof(pricesJob),
                                e);
                        }
                    }

                    OnStopped();
                });
            }
        }

        private DateTime lastOrdersCheckTime = DateTime.UtcNow;

        private async Task CheckExecutedOrders()
        {
            var newTime = DateTime.UtcNow;
            var executed = await GetExecutedOrders(lastOrdersCheckTime, TimeSpan.FromSeconds(5));
            lastOrdersCheckTime = newTime;

            foreach (var executedTrade in executed)
            {
                await _tradeHandler.Handle(executedTrade);
            }
        }

        protected override void StopImpl()
        {
            ctSource.Cancel();
        }

        private async Task<bool> CheckServerTime(CancellationToken cancellationToken)
        {
            var serverTime = await publicData.GetServerTime(cancellationToken);
            var now = DateTime.UtcNow;
            long differenceTicks = Math.Abs(serverTime.FromUnixTime.Ticks - now.Ticks);
            bool differenceInThreshold = differenceTicks <= TimeSpan.FromMinutes(2).Ticks;

            await LykkeLog.WriteInfoAsync(
                nameof(Kraken),
                nameof(KrakenExchange),
                nameof(pricesJob),
                $"Server time: {serverTime.FromUnixTime}; now: {now}; difference ticks: {differenceTicks}. In threshold: {differenceInThreshold}");

            return differenceInThreshold;
        }

        public override async Task<IEnumerable<AccountBalance>> GetAccountBalance(TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            return (await privateData.GetAccountBalance(null, cts.Token))
                .Select(x => new AccountBalance()
                {
                    Asset = x.Key,
                    Balance = x.Value
                });
        }

        public override async Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);

            var orderInfo = await privateData.AddOrder(signal, translatedSignal, cts.Token);
            string txId = orderInfo.TxId.FirstOrDefault();
            translatedSignal.ExternalId = txId;

            return new ExecutionReport(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, OrderExecutionStatus.New);
        }

        public override async Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var result = await privateData.CancelOrder(signal.OrderId, translatedSignal);

            var executedTrade = new ExecutionReport(signal.Instrument,
                DateTime.UtcNow,
                signal.Price ?? 0,
                signal.Volume,
                signal.TradeType,
                signal.OrderId,
                result.Pending ? OrderExecutionStatus.Pending : OrderExecutionStatus.Cancelled);

            translatedSignal.SetExecutionResult(executedTrade);

            return executedTrade;
        }

        public override async Task<IEnumerable<ExecutionReport>> GetOpenOrders(TimeSpan timeout)
        {
            return (await privateData.GetOpenOrders(new CancellationTokenSource(timeout).Token))
                .Select(x => new ExecutionReport(new Instrument(Name, x.Value.DescriptionInfo.Pair),
                    DateTimeUtils.FromUnix(x.Value.StartTime),
                    x.Value.Price,
                    x.Value.Volume,
                    x.Value.DescriptionInfo.Type == TradeDirection.Buy ? TradeType.Buy : TradeType.Sell,
                    x.Key,
                    ConvertStatus(x.Value.Status)));
        }

        public override StreamingSupport StreamingSupport => new StreamingSupport(false, false, false);

        public async Task<IEnumerable<ExecutionReport>> GetExecutedOrders(DateTime start, TimeSpan timeout)
        {
            return (await privateData.GetClosedOrders(start, new CancellationTokenSource(timeout).Token)).Closed
                .Select(x => new ExecutionReport(new Instrument(Name, x.Value.DescriptionInfo.Pair),
                    DateTimeUtils.FromUnix(x.Value.StartTime),
                    x.Value.Price,
                    x.Value.Volume,
                    x.Value.DescriptionInfo.Type == TradeDirection.Buy ? TradeType.Buy : TradeType.Sell,
                    x.Key,
                    ConvertStatus(x.Value.Status)));
        }

        private OrderExecutionStatus ConvertStatus(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Pending:
                    return OrderExecutionStatus.Pending;
                case OrderStatus.Open:
                    return OrderExecutionStatus.New;
                case OrderStatus.Closed:
                    return OrderExecutionStatus.Fill;
                case OrderStatus.Canceled:
                    return OrderExecutionStatus.Cancelled;
                case OrderStatus.Expired:
                    return OrderExecutionStatus.Rejected;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
