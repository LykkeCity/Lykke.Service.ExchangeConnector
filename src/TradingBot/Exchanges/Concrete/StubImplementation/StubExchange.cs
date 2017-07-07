using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingBot.Common.Trading;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    public class StubExchange : Exchange
    {
	    private readonly ILogger logger = Logging.CreateLogger<StubExchange>();
	    private readonly StubExchangeConfiguration config;
	    

        public StubExchange(StubExchangeConfiguration config)
	        : base("Stub Exchange Implementation", config)
        {
            this.config = config;
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
		}


        private bool closePricesStreamRequested;
        private Task streamJob;

		public override Task OpenPricesStream()
		{
            closePricesStreamRequested = false;
            var random = new Random();
		    
		    var nPoints = 10000000;
		    
            var gbms = 
                Instruments.ToDictionary(x => x, 
                x => new GeometricalBrownianMotion(1.0, 0.25, 1.0, nPoints, 0, random));

		    streamJob = Task.Run(async () =>
		    {
			    while (!closePricesStreamRequested)
			    {
				    foreach (var instrument in Instruments)
				    {       
					    var currentPrices =
						    Enumerable.Range(0, config.PricesPerInterval)
							    .Select(x => Math.Round((decimal) gbms[instrument].GenerateNextValue(), 4))
							    .Select(x => new TickPrice(DateTime.UtcNow, x))
							    .ToArray();

					    lock (ActualSignalsSyncRoot)
					    {
						    var trades = new List<ExecutedTrade>();
					    
						    foreach (var tradingSignal in ActualSignals[instrument.Name])
						    {
							    if (tradingSignal.TradeType == TradeType.Buy
							        && currentPrices.Any(x => x.Ask <= tradingSignal.Price))
							    {
								    var trade = new ExecutedTrade(DateTime.UtcNow, 
									    currentPrices.First(x => x.Ask <= tradingSignal.Price).Ask,
									    tradingSignal.Count,
									    TradeType.Buy);
							    
								    trades.Add(trade);
							    }
							    else if (tradingSignal.TradeType == TradeType.Sell
							             && currentPrices.Any(x => x.Bid >= tradingSignal.Price))
							    {
								    var trade = new ExecutedTrade(DateTime.UtcNow,
									    currentPrices.First(x => x.Bid >= tradingSignal.Price).Bid,
									    tradingSignal.Count,
									    TradeType.Sell);

								    trades.Add(trade);
							    }
						    }
					    
						    trades.ForEach(x => Positions[instrument.Name].AddTrade(x));    
					    }
					    
					    // TODO: deal with awaitable. I don't want to wait here for Azure and Rabbit connections
					    await CallHandlers(new InstrumentTickPrices(instrument, currentPrices));
						
//					    // TODO: translate executed trades back to Alpha Engine
//					    //ExecutedTrades[instrument.Name].AddRange(trades);
				    }
                
				    await Task.Delay(config.PricesIntervalInMilliseconds);
			    } 
		    });

			return streamJob;
		}
	    
        public override void ClosePricesStream()
        {
            closePricesStreamRequested = true;
	        streamJob?.Wait();
        }
    }
}
