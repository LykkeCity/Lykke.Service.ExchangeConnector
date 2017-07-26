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
	    public new static readonly string Name = "stub";
	    
	    private readonly ILogger logger = Logging.CreateLogger<StubExchange>();
	    private readonly StubExchangeConfiguration config;
	    

        public StubExchange(StubExchangeConfiguration config)
	        : base(Name, config)
        {
            this.config = config;
        }

        protected override Task<bool> TestConnectionImpl(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
		}


        private bool closePricesStreamRequested;
        private Task streamJob;

	    private long counter = 0;

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
							    .Select(x => Math.Round((decimal) gbms[instrument].GenerateNextValue(), 6))
							    .Select(x => new TickPrice(DateTime.UtcNow, x))
							    .ToArray();

					    lock (ActualSignalsSyncRoot)
					    {
						    decimal lowestAsk = currentPrices.Min(x => x.Ask);
						    decimal highestBid = currentPrices.Max(x => x.Bid);
						    
						    var trades = new List<ExecutedTrade>();
						    var executedOrders = new List<TradingSignal>();
					    
						    foreach (var tradingSignal in ActualSignals[instrument.Name].Where(x => x.Count > 0))
						    {
							    if (tradingSignal.TradeType == TradeType.Buy
							        && lowestAsk <= tradingSignal.Price)
							    {
								    var trade = new ExecutedTrade(
									    instrument,
									    DateTime.UtcNow, 
									    lowestAsk,
									    tradingSignal.Count,
									    TradeType.Buy,
									    tradingSignal.OrderId,
									    ExecutionStatus.Fill);
							    
								    trades.Add(trade);
								    executedOrders.Add(tradingSignal);
								    
								    logger.LogDebug($"EXECUTION of order {tradingSignal} by price {trade.Price}");
							    }
							    else if (tradingSignal.TradeType == TradeType.Sell
							             && highestBid >= tradingSignal.Price)
							    {
								    var trade = new ExecutedTrade(instrument,
									    DateTime.UtcNow,
									    highestBid,
									    tradingSignal.Count,
									    TradeType.Sell,
									    tradingSignal.OrderId,
									    ExecutionStatus.Fill);
    
								    trades.Add(trade);
								    executedOrders.Add(tradingSignal);
								    
								    logger.LogDebug($"EXECUTION of order {tradingSignal} by price {trade.Price}");
							    }
						    }

						    foreach (var signal in executedOrders)
						    {
							    ActualSignals[instrument.Name].Remove(signal);
							    logger.LogDebug($"Trading order {signal} was removed from actual signals as executed");
						    }

							trades.ForEach(async x =>
							    {
								    Positions[instrument.Name].AddTrade(x);
								    await CallExecutedTradeHandlers(x);
							    });
						    
						    foreach (var currentPrice in currentPrices)
						    {
							    if (++counter % 100 == 0)
							    {
								    logger.LogDebug($"Step {counter}, total PnL: {Positions[instrument.Name].GetPnL(currentPrice.Mid)}");
							    }    
						    }
					    }
					    
					    // TODO: deal with awaitable. I don't want to wait here for Azure and Rabbit connections
					    await CallHandlers(new InstrumentTickPrices(instrument, currentPrices));
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

	    protected override Task<bool> AddOrder(Instrument instrument, TradingSignal signal)
	    {
		    return Task.FromResult(true);
	    }

	    protected override async Task<bool> CancelOrder(Instrument instrument, TradingSignal signal)
	    {
		    await CallExecutedTradeHandlers(new ExecutedTrade(
			    instrument,
			    DateTime.UtcNow, signal.Price, signal.Count, signal.TradeType,
			    signal.OrderId, ExecutionStatus.Cancelled));

		    return true;
	    }
    }
}
