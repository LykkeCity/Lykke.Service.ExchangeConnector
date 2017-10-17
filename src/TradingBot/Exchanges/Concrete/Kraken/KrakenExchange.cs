using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Kraken
{
    internal class KrakenExchange : Exchange
    {
        public new static readonly string Name = "kraken";

        private readonly KrakenConfig config;

        private readonly PublicData publicData;
        private readonly PrivateData privateData;

        private Task pricesJob;
        private CancellationTokenSource ctSource;

        public KrakenExchange(KrakenConfig config, TranslatedSignalsRepository translatedSignalsRepository, ILog log) :
            base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;

            var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(3) }; // TODO: HttpClient have to be Singleton
            publicData = new PublicData(new ApiClient(httpClient, log));
            privateData = new PrivateData(new ApiClient(new HttpClient() { Timeout = TimeSpan.FromSeconds(30) }, log), config.ApiKey, config.PrivateKey, new NonceProvider());
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

            if (config.PubQuotesToRabbit || config.SaveQuotesToAzure)
            {
                pricesJob = Task.Run(async () =>
                {
                    var lasts = Instruments.Select(x => (long)0).ToList();

                    while (!ctSource.IsCancellationRequested)
                    {
                        try
                        {
                            for (int i = 0; i < Instruments.Count && !ctSource.IsCancellationRequested; i++)
                            {
                                SpreadDataResult result;

                                try
                                {
                                    result = await publicData.GetSpread(ctSource.Token, Instruments[i].Name, lasts[i]);
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

                                lasts[i] = result.Last;
                                var prices = result.Data.Single().Value.Select(x => new TickPrice(x.Time, x.Ask, x.Bid)).ToArray();

                                if (prices.Any())
                                {
                                    if (prices.Length == 1 && prices[0].Time == DateTimeUtils.FromUnix(lasts[i]))
                                    {
                                        // If there is only one price and it has timestamp of last one, ignore it.
                                    }
                                    else
                                    {
                                        await CallTickPricesHandlers(new InstrumentTickPrices(Instruments[i], prices));
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
                await CallExecutedTradeHandlers(executedTrade);
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


        protected override async Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            var executedTrade = await AddOrderAndWaitExecution(instrument, signal, translatedSignal, TimeSpan.FromSeconds(30));

            if (executedTrade == null) return false;

            translatedSignal.SetExecutionResult(executedTrade);

            return true;
        }

        protected override async Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            var executedTrade = await CancelOrderAndWaitExecution(instrument, signal, translatedSignal, TimeSpan.FromSeconds(30));

            return executedTrade.Status == ExecutionStatus.Cancelled;
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);

            var orderInfo = await privateData.AddOrder(instrument, signal, translatedSignal, cts.Token);
            string txId = orderInfo.TxId.FirstOrDefault();
            translatedSignal.ExternalId = txId;

            return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId, ExecutionStatus.New);
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            var result = await privateData.CancelOrder(signal.OrderId, translatedSignal);

            var executedTrade = new ExecutedTrade(instrument,
                DateTime.UtcNow,
                signal.Price ?? 0,
                signal.Volume,
                signal.TradeType,
                signal.OrderId,
                result.Pending ? ExecutionStatus.Pending : ExecutionStatus.Cancelled);

            translatedSignal.SetExecutionResult(executedTrade);

            return executedTrade;
        }

        public override async Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            return (await privateData.GetOpenOrders(new CancellationTokenSource(timeout).Token))
                .Select(x => new ExecutedTrade(new Instrument(Name, x.Value.DescriptionInfo.Pair), 
                    DateTimeUtils.FromUnix(x.Value.StartTime), 
                    x.Value.Price,
                    x.Value.Volume,
                    x.Value.DescriptionInfo.Type == TradeDirection.Buy ? TradeType.Buy : TradeType.Sell,
                    x.Key,
                    ConvertStatus(x.Value.Status)));
        }

        public async Task<IEnumerable<ExecutedTrade>> GetExecutedOrders(DateTime start, TimeSpan timeout)
        {
            return (await privateData.GetClosedOrders(start, new CancellationTokenSource(timeout).Token)).Closed
                .Select(x => new ExecutedTrade(new Instrument(Name, x.Value.DescriptionInfo.Pair), 
                    DateTimeUtils.FromUnix(x.Value.StartTime), 
                    x.Value.Price,
                    x.Value.Volume,
                    x.Value.DescriptionInfo.Type == TradeDirection.Buy ? TradeType.Buy : TradeType.Sell,
                    x.Key,
                    ConvertStatus(x.Value.Status)));
        }

        private ExecutionStatus ConvertStatus(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Pending:
                    return ExecutionStatus.Pending;
                case OrderStatus.Open:
                    return ExecutionStatus.New;
                case OrderStatus.Closed:
                    return ExecutionStatus.Fill;
                case OrderStatus.Canceled:
                    return ExecutionStatus.Cancelled;
                case OrderStatus.Expired:
                    return ExecutionStatus.Rejected;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
