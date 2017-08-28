using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Kraken
{
    public class KrakenExchange : Exchange
    {
        public new static readonly string Name = "kraken";
        
        private readonly KrakenConfig config;

        public KrakenExchange(KrakenConfig config) : base(Name, config)
        {
            this.config = config;
            
            var httpClient = new HttpClient() {Timeout = TimeSpan.FromSeconds(3)}; // TODO: HttpClient have to be Singleton
            publicData = new PublicData(new ApiClient(httpClient));
            privateData = new PrivateData(new ApiClient(new HttpClient() {Timeout = TimeSpan.FromSeconds(30)}), config.ApiKey, config.PrivateKey, new NonceProvider());
        }

        private readonly PublicData publicData;
        private readonly PrivateData privateData;
        
        protected override async Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            var serverTime = await publicData.GetServerTime(cancellationToken);
            var now = DateTime.UtcNow;
            long differenceTicks = Math.Abs(serverTime.FromUnixTime.Ticks - now.Ticks);
            bool differenceInThreshold = differenceTicks <= TimeSpan.FromMinutes(2).Ticks;

            Logger.LogDebug($"Server time: {serverTime.FromUnixTime}; now: {now}; difference ticks: {differenceTicks}. In threshold: {differenceInThreshold}");

            return differenceInThreshold;
        }

        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();

        public override async Task OpenPricesStream()
        {
            var token = ctSource.Token;

            var lasts = Instruments.Select(x => (long)0).ToList();

            while(!token.IsCancellationRequested)
            {
                for (int i = 0; i < Instruments.Count && !token.IsCancellationRequested; i++)
                {

                    SpreadDataResult result;

                    try
                    {
						result = await publicData.GetSpread(token, Instruments[i].Name, lasts[i]);
					}
                    catch (Exception e)
                    {
                        Logger.LogError(0, e, "Can't get prices from kraken.");
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
                            await CallHandlers(new InstrumentTickPrices(Instruments[i], prices));
						}
					}

					await Task.Delay(TimeSpan.FromSeconds(4), token);
                }
            }
        }

        public override Task<Dictionary<string, decimal>> GetAccountBalance(CancellationToken cancellationToken)
        {
            return privateData.GetAccountBalance(cancellationToken);
        }

        public Task<TradeBalanceInfo> GetTradeBalance(CancellationToken cancellationToken)
        {
            return privateData.GetTradeBalance(cancellationToken);
        }

        public override void ClosePricesStream()
        {
            ctSource.Cancel();
        }

        protected override async Task<bool> AddOrder(Instrument instrument, TradingSignal signal)
        {
            var executedTrade = await AddOrderAndWaitExecution(instrument, signal, TimeSpan.FromSeconds(30));

            return executedTrade != null;
        }

        protected override async Task<bool> CancelOrder(Instrument instrument, TradingSignal signal)
        {
            var executedTrade = await CancelOrderAndWaitExecution(instrument, signal, TimeSpan.FromSeconds(30));

            return executedTrade.Status == ExecutionStatus.Cancelled;
        }

        public override async Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            
            var orderInfo = await privateData.AddOrder(instrument, signal, cts.Token);
            
            return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType, orderInfo.TxId.FirstOrDefault(), ExecutionStatus.New);
        }

        public override async Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TimeSpan timeout)
        {
            var result = await privateData.CancelOrder(signal.OrderId);
            
            return new ExecutedTrade(instrument, DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType, signal.OrderId, result.Pending ? ExecutionStatus.Pending : ExecutionStatus.Cancelled);
        }

        public Task<string> GetOpenOrders(CancellationToken cancellationToken)
        {
            return privateData.GetOpenOrders(cancellationToken);
        }
    }
}
