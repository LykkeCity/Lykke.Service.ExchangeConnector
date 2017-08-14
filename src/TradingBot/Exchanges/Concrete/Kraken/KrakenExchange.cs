using System;
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
        }

        private readonly PublicData publicData = new PublicData(new ApiClient(new HttpClient() { Timeout = TimeSpan.FromSeconds(3)}));

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
                        Logger.LogError(new EventId(), e, "Can't get prices from kraken.");
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

        public override void ClosePricesStream()
        {
            ctSource.Cancel();
        }

        protected override Task<bool> AddOrder(Instrument instrument, TradingSignal signal)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> CancelOrder(Instrument instrument, TradingSignal signal)
        {
            throw new NotImplementedException();
        }
    }
}
