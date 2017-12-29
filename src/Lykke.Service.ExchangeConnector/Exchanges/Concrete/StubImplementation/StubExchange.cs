using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Exchanges.Abstractions;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.StubImplementation
{
    internal class StubExchange : Exchange
    {
	    public new static readonly string Name = "stub";
	    
	    private readonly StubExchangeConfiguration config;
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<ExecutedTrade> _tradeHandler;

        protected IReadOnlyDictionary<string, Position> Positions { get; }

        protected readonly Dictionary<string, LinkedList<TradingSignal>> ActualSignals;
        
        private readonly object syncRoot = new object();

        public StubExchange(StubExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository,IHandler<TickPrice> tickPriceHandler, IHandler<ExecutedTrade> tradeHandler, ILog log)
	        : base(Name, config, translatedSignalsRepository, log)
        {
            this.config = config;
            _tickPriceHandler = tickPriceHandler;
            _tradeHandler = tradeHandler;

            decimal initialValue = 100m;
            Positions = Instruments.ToDictionary(x => x.Name, x => new Position(x, initialValue));
            ActualSignals = Instruments.ToDictionary(x => x.Name, x => new LinkedList<TradingSignal>());
        }

        private CancellationTokenSource ctSource;
        private Task streamJob;
        private int counter = 0;

		protected override void StartImpl()
		{
            ctSource = new CancellationTokenSource();
            var random = new Random();
		    
		    var nPoints = 10000000;
		    
            var gbms = 
                Instruments.ToDictionary(x => x, 
                x => new GeometricalBrownianMotion(1.0, 0.95, 1.0, nPoints, 0, random));

		    streamJob = Task.Run(async () =>
		    {
			    OnConnected();
			    
			    while (!ctSource.IsCancellationRequested)
			    {
				    foreach (var instrument in Instruments)
				    {       
				        try
				        {
                            var currentTickPrice = new TickPrice(instrument, DateTime.UtcNow, Math.Round((decimal) gbms[instrument].GenerateNextValue(), 6));
				        
					    lock (syncRoot)
					    {
						    var trades = new List<OrderStatusUpdate>();
						    var executedOrders = new List<TradingSignal>();
					    
						    foreach (var tradingSignal in ActualSignals[instrument.Name].Where(x => x.Volume > 0))
						    {
							    if (tradingSignal.TradeType == TradeType.Buy
                                        && currentTickPrice.Ask <= tradingSignal.Price)
							    {
								    var trade = new OrderStatusUpdate(
									    instrument,
									    DateTime.UtcNow, 
                                            tradingSignal.Price.Value,
									    tradingSignal.Volume,
									    TradeType.Buy,
									    tradingSignal.OrderId,
									    OrderExecutionStatus.Fill);
							    
								    trades.Add(trade);
								    executedOrders.Add(tradingSignal);

							        LykkeLog.WriteInfoAsync(nameof(StubExchange), nameof(StartImpl), nameof(streamJob),
							            $"EXECUTION of order {tradingSignal} by price {trade.Price}").Wait();
							    }
							    else if (tradingSignal.TradeType == TradeType.Sell
                                             && currentTickPrice.Bid >= tradingSignal.Price)
							    {
								    var trade = new OrderStatusUpdate(instrument,
									    DateTime.UtcNow,
                                            tradingSignal.Price.Value,
									    tradingSignal.Volume,
									    TradeType.Sell,
									    tradingSignal.OrderId,
									    OrderExecutionStatus.Fill);
    
								    trades.Add(trade);
								    executedOrders.Add(tradingSignal);
								    
                                        LykkeLog.WriteInfoAsync(nameof(StubExchange), nameof(streamJob), trade.ToString(),
							            $"EXECUTION of order {tradingSignal} by price {trade.Price}").Wait();
							    }
						    }

						    foreach (var signal in executedOrders)
						    {
							    ActualSignals[instrument.Name].Remove(signal);
						        LykkeLog.WriteInfoAsync(nameof(StubExchange), nameof(StartImpl), nameof(streamJob),
						            $"Trading order {signal} was removed from actual signals as executed").Wait();
						    }

							trades.ForEach(async x =>
							    {
								    Positions[instrument.Name].AddTrade(x);
                                        try
                                        {
								    await _tradeHandler.Handle(x);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e);
                                        }
							    });
						    
                                
							    if (++counter % 100 == 0)
							    {
							        LykkeLog.WriteInfoAsync(nameof(StubExchange), nameof(StartImpl), nameof(streamJob),
                                        $"Step {counter}, total PnL: {Positions[instrument.Name].GetPnL(currentTickPrice.Mid)}").Wait();
						    }
                                
					    }
					    
					    // TODO: deal with awaitable. I don't want to wait here for Azure and Rabbit connections
                            await _tickPriceHandler.Handle(currentTickPrice);
				        }
				        catch (Exception e)
				        {
				            Console.WriteLine(e);
				        }
				        
				        await Task.Delay(config.PricesIntervalInMilliseconds, ctSource.Token);
				    }
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

	    private static readonly Random Random = new Random();
	    private void SimulateException()
	    {
		    if (Random.NextDouble() <= 0.2)
		    {
			    throw new Exception("Whoops! This exception is simulated!");
		    }
	    }

        private void SimulateWork()
        {
            Thread.Sleep(TimeSpan.FromSeconds(Random.Next(1, 11)));
        }

	    public override Task<OrderStatusUpdate> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
	    {
	        translatedSignal.RequestSentMessage("stub exchange don't send actual request");
//
//	        SimulateWork();
//	        SimulateException();
//	        
	        translatedSignal.ResponseReceived("stub exchange don't recevie actual response");
	        lock (syncRoot)
	        {
	            var s = new TradingSignal(signal.Instrument, Guid.NewGuid().ToString(), signal.Command, signal.TradeType, signal.Price, signal.Volume, signal.Time, signal.OrderType, signal.TimeInForce);
	            ActualSignals[signal.Instrument.Name].AddLast(s);
	            translatedSignal.ExternalId = s.OrderId;
	            
	            return Task.FromResult(new OrderStatusUpdate(s.Instrument, DateTime.UtcNow, s.Price ?? 0, s.Volume, s.TradeType, s.OrderId, OrderExecutionStatus.New));
	        }
	    }

        public override Task<OrderStatusUpdate> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout)
        {
            translatedSignal.RequestSentMessage("stub exchange don't send actual request");
//	        SimulateWork();
//	        SimulateException();
            translatedSignal.ResponseReceived("stub exchange don't recevie actual response");

            bool isCanceled = false;
            lock (syncRoot)
            {
                TradingSignal existing = ActualSignals[signal.Instrument.Name].FirstOrDefault(x => x.OrderId == signal.OrderId);
                if (existing != null)
                {
                    ActualSignals[signal.Instrument.Name].Remove(existing);
                    isCanceled = true;
                }
            }

            return Task.FromResult(new OrderStatusUpdate(signal.Instrument, DateTime.UtcNow, signal.Price ?? 0, signal.Volume, signal.TradeType, signal.OrderId,
                isCanceled ? OrderExecutionStatus.Cancelled : OrderExecutionStatus.Unknown));
        }
    }
}
