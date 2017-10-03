using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.Extensions.Logging;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    internal class StubExchange : Exchange
    {
	    public new static readonly string Name = "stub";
	    
	    private readonly ILogger logger = Logging.CreateLogger<StubExchange>();
	    private readonly StubExchangeConfiguration config;
	    

        public StubExchange(StubExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, ILog log)
	        : base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
        }

        private CancellationTokenSource ctSource;
        private Task streamJob;

	    private long counter = 0;

		protected override void StartImpl()
		{
            ctSource = new CancellationTokenSource();
            var random = new Random();
		    
		    var nPoints = 10000000;
		    
            var gbms = 
                Instruments.ToDictionary(x => x, 
                x => new GeometricalBrownianMotion(1.0, 0.25, 1.0, nPoints, 0, random));

		    streamJob = Task.Run(async () =>
		    {
			    OnConnected();
			    
			    while (!ctSource.IsCancellationRequested)
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
					    
						    foreach (var tradingSignal in ActualSignals[instrument.Name].Where(x => x.Volume > 0))
						    {
							    if (tradingSignal.TradeType == TradeType.Buy
							        && lowestAsk <= tradingSignal.Price)
							    {
								    var trade = new ExecutedTrade(
									    instrument,
									    DateTime.UtcNow, 
									    lowestAsk,
									    tradingSignal.Volume,
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
									    tradingSignal.Volume,
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
                
				    await Task.Delay(config.PricesIntervalInMilliseconds, ctSource.Token);
			    } 
			    
			    OnStopped();
		    });
		}
	    
        protected override void StopImpl()
        {
	        if (ctSource != null && streamJob != null)
	        {
		        ctSource.Cancel();
	        }
        }

	    protected override Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
	    {
		    translatedSignal.RequestSent("stub exchange don't send actual request");
		    SimulateException();
		    translatedSignal.ResponseReceived("stub exchange don't recevie actual response");

		    return Task.FromResult(true);
	    }

	    private static readonly Random Random = new Random();
	    private void SimulateException()
	    {
		    if (Random.NextDouble() <= 0.2)
		    {
			    throw new Exception("Whoops! This exception is simulated!");
		    }
	    }

	    protected override async Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
	    {
		    translatedSignal.RequestSent("stub exchange don't send actual request");
		    SimulateException();
		    translatedSignal.ResponseReceived("stub exchange don't recevie actual response");
		    
		    await CallExecutedTradeHandlers(new ExecutedTrade(
			    instrument,
			    DateTime.UtcNow, signal.Price, signal.Volume, signal.TradeType,
			    signal.OrderId, ExecutionStatus.Cancelled));

		    return true;
	    }

	    public override Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
	    {
		    throw new NotImplementedException();
	    }

	    public override Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
	    {
		    throw new NotImplementedException();
	    }
    }
}
