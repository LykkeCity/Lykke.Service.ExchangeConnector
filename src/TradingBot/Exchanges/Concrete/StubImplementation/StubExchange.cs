using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;

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

		public override async Task OpenPricesStream(Action<InstrumentTickPrices> callback)
		{
            closePricesStreamRequested = false;
            //random = new Random();
		    var nPoints = 10000000;
            var gbms = 
                Instruments.ToDictionary(x => x, 
                x => new GeometricalBrownianMotion(1.0, 0.25, 1.0, nPoints, 0));
		    
            while (!closePricesStreamRequested)
            {
                foreach (var instrument in Instruments)
                {                
//                    bool grow = random.NextDouble() >= 0.5;
//                    decimal percents = (decimal) random.NextDouble() / 100;
//                    decimal delta = prices[instrument] * percents;
//
//                    if (grow)
//                        prices[instrument] += delta;
//                    else
//                        prices[instrument] -= delta;

                    var currentPrices =
                        Enumerable.Range(0, config.PricesPerInterval)
                            .Select(x => //Math.Round(
                                (decimal) gbms[instrument].GenerateNextValue()
                                //, 4)
                                )
                            .Select(x => new TickPrice(DateTime.Now, x))
                            .ToArray();
                    
                    callback(new InstrumentTickPrices(instrument, currentPrices));
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
