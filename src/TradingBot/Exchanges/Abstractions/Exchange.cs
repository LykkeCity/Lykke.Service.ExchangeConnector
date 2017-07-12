using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Common.Infrastructure;
using TradingBot.Common.Trading;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Abstractions
{
    public abstract class Exchange
    {
        protected ILogger Logger = Logging.CreateLogger<Exchange>();

        private readonly List<TickPriceHandler> tickPriceHandlers = new List<TickPriceHandler>();

        private readonly List<ExecutedOrdersHandler> executedTradeHandlers = new List<ExecutedOrdersHandler>();

        public string Name { get; }

        protected Exchange(string name, 
            IExchangeConfiguration config)
        {
            decimal initialValue = 100m; // TODO: get initial value from config? or get if from real exchange.
            this.Name = name;

            if (config.Instruments == null || config.Instruments.Length == 0)
            {
                throw new ArgumentException($"There is no instruments in the settings for {name} exchange");
            }
            
            Instruments = config.Instruments.Select(x => new Instrument(x)).ToList();
            Positions = Instruments.ToDictionary(x => x.Name, x => new Position(x, initialValue));

            AllSignals = Instruments.ToDictionary(x => x.Name, x => new List<TradingSignal>());
            ActualSignals = Instruments.ToDictionary(x => x.Name, x => new LinkedList<TradingSignal>());
            ExecutedTrades = Instruments.ToDictionary(x => x.Name, x => new List<ExecutedTrade>());
        }

        public void AddTickPriceHandler(TickPriceHandler handler)
        {
            tickPriceHandlers.Add(handler);
        }

        public void AddExecutedTradeHandler(ExecutedOrdersHandler handler)
        {
            executedTradeHandlers.Add(handler);
        }

        public IReadOnlyList<Instrument> Instruments { get; }
        
        public IReadOnlyDictionary<string, Position> Positions { get; }

        protected Dictionary<string, List<TradingSignal>> AllSignals;
        protected Dictionary<string, LinkedList<TradingSignal>> ActualSignals;
        protected Dictionary<string, List<ExecutedTrade>> ExecutedTrades;
        
        public Task<bool> TestConnection()
        {
            return TestConnection(CancellationToken.None);
        }

        public async Task<bool> TestConnection(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Trying to test connection...");

            try
            {
				bool result = await TestConnectionImpl(cancellationToken);

				if (result)
				{
					Logger.LogInformation("Connection tested successfully.");
				}
				else
				{
					Logger.LogError("Connection test failed.");
				}

				return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(new EventId(), ex, "Connection test failed with error");
                return false;
            }

        }

        protected abstract Task<bool> TestConnectionImpl(CancellationToken cancellationToken);

        //public abstract Task<AccountInfo> GetAccountInfo(CancellationToken cancellationToken);


        public abstract Task OpenPricesStream();

        protected Task CallHandlers(InstrumentTickPrices tickPrices)
        {
            return Task.WhenAll(tickPriceHandlers.Select(x => x.Handle(tickPrices)));
        }

        protected Task CallExecutedTradeHandlers(ExecutedTrade trade)
        {
            return Task.WhenAll(executedTradeHandlers.Select(x => x.Handle(trade)));
        }

        public abstract void ClosePricesStream();

        
        protected readonly object ActualSignalsSyncRoot = new object();
        
        public virtual Task PlaceTradingOrders(InstrumentTradingSignals signals)
        {
            lock (ActualSignalsSyncRoot)
            {
                bool added = false;
                
                foreach (var arrivedSignal in signals.TradingSignals)
                {
                    if (!ActualSignals[signals.Instrument.Name].Any(x => x.Equals(arrivedSignal)))
                    {
                        ActualSignals[signals.Instrument.Name].AddLast(arrivedSignal);
                        added = true;
                    }
                }

                var toRemove = new List<TradingSignal>();
                
                foreach (var existingSignal in ActualSignals[signals.Instrument.Name])
                {
                    if (!signals.TradingSignals.Any(x => existingSignal.Equals(x)))
                    {
                        toRemove.Add(existingSignal);
                    }
                }

                if (toRemove.Any())
                {
                    foreach (var signal in toRemove)
                    {
                        ActualSignals[signals.Instrument.Name].Remove(signal);
                        //Logger.LogDebug($"Removed non actual signal: {signal}");
                    }
                }

                if (added)
                {
                    Logger.LogDebug($"Current orders:\n {string.Join("\n", ActualSignals[signals.Instrument.Name])}");
                }
            }
            
            return Task.FromResult(0);            
        }
    }
}
