using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Exchanges.Concrete.Kraken.Endpoints;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Kraken
{
    public class KrakenExchange : Exchange
    {
        public KrakenExchange() : base("Kraken")
        {
        }

        private readonly PublicData PublicData = new PublicData(new ApiClient(new HttpClient()));

        protected override async Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            var serverTime = await PublicData.GetServerTime(cancellationToken);
            var now = DateTime.UtcNow;
            long differenceTicks = Math.Abs(serverTime.FromUnixTime.Ticks - now.Ticks);
            bool differenceInThreshold = differenceTicks <= TimeSpan.FromMinutes(2).Ticks;

            Logger.LogDebug($"Server time: {serverTime.FromUnixTime}; now: {now}; difference ticks: {differenceTicks}. In threshold: {differenceInThreshold}");

            return differenceInThreshold;
        }

        private readonly CancellationTokenSource ctSource = new CancellationTokenSource();

        public override async Task OpenPricesStream(Instrument[] instruments, Action<TickPrice[]> callback)
        {
            if (instruments.Length != 1)
                throw new ApiException("Kraken supports only one instrument at a time");

            var token = ctSource.Token;
            long last = 0;
            while(!token.IsCancellationRequested)
            {
                var result = await PublicData.GetSpread(token, instruments.Single().Name, last);
                last = result.Last;

                var prices = result.Data.Single().Value.Select(x => new TickPrice(x.Time, x.Ask, x.Bid)).ToArray();

                if (prices.Any())
                {
                    callback(prices);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        public override void ClosePricesStream()
        {
            ctSource.Cancel();
        }
    }
}
