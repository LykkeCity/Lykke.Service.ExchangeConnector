using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public class StubExchange : Exchange
    {
        private readonly StubExchangeConfiguration config;

        public StubExchange(StubExchangeConfiguration config) : base("Stub Exchange Implementation", config)
        {
            this.config = config;
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
		}


        private bool closePricesStreamRequested;
        private Random random;

		public override async Task OpenPricesStream(Action<InstrumentTickPrices> callback)
		{
            closePricesStreamRequested = false;
            random = new Random();

		    var prices = Instruments.ToDictionary(x => x, x => 100m);

            while (!closePricesStreamRequested)
            {
                foreach (var instrument in Instruments)
                {                
                    bool grow = random.NextDouble() >= 0.5;
                    decimal percents = (decimal) random.NextDouble() / 100;
                    decimal delta = prices[instrument] * percents;

                    if (grow)
                        prices[instrument] += delta;
                    else
                        prices[instrument] -= delta;

                    prices[instrument] = Math.Round(prices[instrument], 4);
                
                    callback(new InstrumentTickPrices(instrument, new[] { new TickPrice(DateTime.Now, prices[instrument]) }));
                }
                
                await Task.Delay(config.PricesIntervalInMilliseconds);
            }
		}


        public override void ClosePricesStream()
        {
            closePricesStreamRequested = true;
        }
    }
}
