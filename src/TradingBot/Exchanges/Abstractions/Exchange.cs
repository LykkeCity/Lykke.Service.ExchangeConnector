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

        private readonly List<TickPriceHandler> handlers = new List<TickPriceHandler>();

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
            ActualSignals = Instruments.ToDictionary(x => x.Name, x => new List<TradingSignal>());
            ExecutedTrades = Instruments.ToDictionary(x => x.Name, x => new List<ExecutedTrade>());
        }

        public void AddHandler(TickPriceHandler handler)
        {
            handlers.Add(handler);
        }

        public IReadOnlyList<Instrument> Instruments { get; }
        
        public IReadOnlyDictionary<string, Position> Positions { get; }

        protected Dictionary<string, List<TradingSignal>> AllSignals;
        protected Dictionary<string, List<TradingSignal>> ActualSignals;
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
            return Task.WhenAll(handlers.Select(x => x.Handle(tickPrices)));
        }

        public abstract void ClosePricesStream();

        
        protected readonly object ActualSignalsSyncRoot = new object();
        public virtual Task PlaceTradingOrders(InstrumentTradingSignals signals)
        {
            //AllSignals[signals.Instrument.Name].AddRange(signals.TradingSignals);

            lock (ActualSignalsSyncRoot)
            {
                ActualSignals[signals.Instrument.Name].Clear();
                ActualSignals[signals.Instrument.Name].AddRange(signals.TradingSignals);
            }
            
            return Task.FromResult(0);            
        }
    }
}
