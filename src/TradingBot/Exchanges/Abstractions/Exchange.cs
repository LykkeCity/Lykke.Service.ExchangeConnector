using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Polly;
using TradingBot.Communications;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Abstractions
{
    public abstract class Exchange
    {
        protected readonly ILogger Logger = Logging.CreateLogger<Exchange>();

        private readonly List<Handler<InstrumentTickPrices>> tickPriceHandlers = new List<Handler<InstrumentTickPrices>>();

        private readonly List<Handler<ExecutedTrade>> executedTradeHandlers = new List<Handler<ExecutedTrade>>();

        private readonly TranslatedSignalsRepository translatedSignalsRepository;
        
        public string Name { get; }

        public IExchangeConfiguration Config { get; }

        public ExchangeState State { get; private set; }

        private readonly TimeSpan tradingSignalsThreshold = TimeSpan.FromMinutes(10);

        protected Exchange(string name, IExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository)
        {
            Name = name;
            Config = config;
            State = ExchangeState.Initializing;

            this.translatedSignalsRepository = translatedSignalsRepository;

            if (config.Instruments == null || config.Instruments.Length == 0)
            {
                throw new ArgumentException($"There is no instruments in the settings for {Name} exchange");
            }

            decimal initialValue = 100m; // TODO: get initial value from config? or get it from real exchange.
            Instruments = config.Instruments.Select(x => new Instrument(Name, x)).ToList();
            Positions = Instruments.ToDictionary(x => x.Name, x => new Position(x, initialValue));
            ActualSignals = Instruments.ToDictionary(x => x.Name, x => new LinkedList<TradingSignal>());
        }

        public void AddTickPriceHandler(Handler<InstrumentTickPrices> handler)
        {
            tickPriceHandlers.Add(handler);
        }

        public void AddExecutedTradeHandler(Handler<ExecutedTrade> handler)
        {
            executedTradeHandlers.Add(handler);
        }

        public void Start()
        {
            if (State != ExchangeState.ErrorState && State != ExchangeState.Stopped && State != ExchangeState.Initializing)
                return;

            State = ExchangeState.Connecting;
            StartImpl();
        }

        protected abstract void StartImpl();
        public event Action Connected;
        protected void OnConnected()
        {
            State = ExchangeState.Connected;
            Connected?.Invoke();
        }

        public void Stop()
        {
            if (State == ExchangeState.Stopped)
                return;

            State = ExchangeState.Stopping;
            StopImpl();
        }

        protected abstract void StopImpl();
        public event Action Stopped;
        protected void OnStopped()
        {
            State = ExchangeState.Stopped;
            Stopped?.Invoke();
        }

        public IReadOnlyList<Instrument> Instruments { get; }

        protected IReadOnlyDictionary<string, Position> Positions { get; }

        protected readonly Dictionary<string, LinkedList<TradingSignal>> ActualSignals;

        protected Task CallHandlers(InstrumentTickPrices tickPrices)
        {
            return Task.WhenAll(tickPriceHandlers.Select(x => x.Handle(tickPrices)));
        }

        protected Task CallExecutedTradeHandlers(ExecutedTrade trade)
        {
            return Task.WhenAll(executedTradeHandlers.Select(x => x.Handle(trade)));
        }

        protected readonly object ActualSignalsSyncRoot = new object();

        private readonly Policy retryThreeTimesPolicy = Policy
            .Handle<Exception>(x => !(x is InsufficientFundsException))
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(3));
        
        public Task HandleTradingSignals(InstrumentTradingSignals signals) // TODO: get rid of whole body lock and make calls async
        {   
            // TODO: check if the Exchange is ready for processing signals, maybe put them into inner queue if readyness is not the case
            
            // TODO: this method should place signals into inner queue only
            // the queue have to be processed via separate worker
            
            
            lock (ActualSignalsSyncRoot)
            {
                if (!ActualSignals.ContainsKey(signals.Instrument.Name))
                {
                    Logger.LogWarning($"ActualSignals doesn't contains a key {signals.Instrument.Name}. It has keys: {string.Join(", ", ActualSignals.Keys)}");
                    return Task.FromResult(0);
                }
                
                foreach (var arrivedSignal in signals.TradingSignals)
                {
                    var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RabbitQueue, signals.Instrument.Exchange, signals.Instrument.Name, arrivedSignal);

                    try
                    {
                        TradingSignal existing;

                        switch (arrivedSignal.Command)
                        {
                            case OrderCommand.Create:
                                try
                                {
                                    if (!arrivedSignal.IsTimeInThreshold(tradingSignalsThreshold))
                                    {
                                        Logger.LogDebug($"Skipping old signal {arrivedSignal}");
                                        translatedSignal.Failure("The signal is too old");
                                        break;
                                    }
                                    
                                    existing = ActualSignals[signals.Instrument.Name]
                                        .SingleOrDefault(x => x.OrderId == arrivedSignal.OrderId);

                                    if (existing != null)
                                        Logger.LogDebug(
                                            $"An order with id {arrivedSignal.OrderId} already in actual signals.");

                                    ActualSignals[signals.Instrument.Name].AddLast(arrivedSignal);

                                    var result = retryThreeTimesPolicy.ExecuteAndCaptureAsync(() =>
                                        AddOrder(signals.Instrument, arrivedSignal, translatedSignal)).Result;
                                    

                                    if (result.Outcome == OutcomeType.Successful)
                                    {
                                        Logger.LogDebug($"Created new order {arrivedSignal}");
                                    }
                                    else
                                    {
                                        ActualSignals[signals.Instrument.Name].Remove(arrivedSignal);
                                        Logger.LogError(0, result.FinalException,
                                            $"Can't create order for {arrivedSignal}");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.LogError(0, e, $"Can't create new order {arrivedSignal}: {e.Message}");
                                    translatedSignal.Failure(e);
                                }


                                break;

                            case OrderCommand.Edit:
                                throw new NotSupportedException("Do not support edit signal");
                            //break;

                            case OrderCommand.Cancel:

                                existing = ActualSignals[signals.Instrument.Name]
                                    .SingleOrDefault(x => x.OrderId == arrivedSignal.OrderId);

                                if (existing != null)
                                {
                                    try
                                    {
                                        ActualSignals[signals.Instrument.Name].Remove(existing);

                                        var result = retryThreeTimesPolicy.ExecuteAndCaptureAsync(() =>
                                            CancelOrder(signals.Instrument, existing, translatedSignal)).Result;

                                        if (result.Outcome == OutcomeType.Successful)
                                        {
                                            Logger.LogDebug($"Canceled order {arrivedSignal}");
                                        }
                                        else
                                        {
                                            Logger.LogError(0, result.FinalException, $"Can't cancel order {existing}");
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.LogError(0, e, $"Can't cancel order {existing.OrderId}: {e.Message}");
                                        translatedSignal.Failure(e);
                                    }
                                }
                                else
                                    Logger.LogWarning($"Command for cancel unexisted order {arrivedSignal}");

                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception e)
                    {
                        translatedSignal.Failure(e);   
                    }
                    finally
                    {
                        translatedSignalsRepository.Save(translatedSignal);
                    }
                    
                }

                if (signals.TradingSignals.Any(x => x.Command == OrderCommand.Create))
                {
                    Logger.LogDebug($"Current orders:\n {string.Join("\n", ActualSignals[signals.Instrument.Name])}");
                }
            }
            
            return Task.FromResult(0);
        }

        protected Task<bool> AddOrder(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            // TODO: save or update TranslatedSignalEntity
            
            return AddOrderImpl(instrument, signal, translatedSignal);
        }
        
        protected abstract Task<bool> AddOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal);
        
        protected Task<bool> CancelOrder(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal)
        {
            // TODO: save or update TranslatedSignalEntity
            
            return CancelOrderImpl(instrument, signal, translatedSignal);
        }
        
        protected abstract Task<bool> CancelOrderImpl(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity trasnlatedSignal);

        
        
        public Dictionary<string, LinkedList<TradingSignal>> ActualOrders => ActualSignals; // TODO: to readonly dictionary and collection

        public abstract Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout);

        public abstract Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout);

        public virtual Task<IEnumerable<AccountBalance>> GetAccountBalance(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<AccountBalance>());
        }
    }
}
