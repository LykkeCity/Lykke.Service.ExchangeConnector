using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using Polly;
using TradingBot.Communications;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Abstractions
{
    public abstract class Exchange
    {
        //protected readonly ILogger Logger = Logging.CreateLogger<Exchange>();

        protected readonly ILog LykkeLog;

        private readonly List<Handler<InstrumentTickPrices>> tickPriceHandlers = new List<Handler<InstrumentTickPrices>>();

        private readonly List<Handler<ExecutedTrade>> executedTradeHandlers = new List<Handler<ExecutedTrade>>();

        private readonly TranslatedSignalsRepository translatedSignalsRepository;
        
        public string Name { get; }

        public IExchangeConfiguration Config { get; }

        public ExchangeState State { get; private set; }

        private readonly TimeSpan tradingSignalsThreshold = TimeSpan.FromMinutes(10);

        private readonly Dictionary<string, object> lockRoots;

        protected Exchange(string name, IExchangeConfiguration config, TranslatedSignalsRepository translatedSignalsRepository, ILog log)
        {
            Name = name;
            Config = config;
            State = ExchangeState.Initializing;
            LykkeLog = log;

            this.translatedSignalsRepository = translatedSignalsRepository;

            if (config.Instruments == null || config.Instruments.Length == 0)
            {
                throw new ArgumentException($"There is no instruments in the settings for {Name} exchange");
            }

            decimal initialValue = 100m; // TODO: get initial value from config? or get it from real exchange.
            Instruments = config.Instruments.Select(x => new Instrument(Name, x)).ToList();
            Positions = Instruments.ToDictionary(x => x.Name, x => new Position(x, initialValue));
            ActualSignals = Instruments.ToDictionary(x => x.Name, x => new LinkedList<TradingSignal>()); // TODO: add external id

            lockRoots = Instruments.ToDictionary(x => x.Name, x => new object());
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
        

        private readonly Policy retryTwoTimesPolicy = Policy
            .Handle<Exception>(x => !(x is InsufficientFundsException))
            .WaitAndRetryAsync(1, attempt => TimeSpan.FromSeconds(3));
        
        public Task HandleTradingSignals(InstrumentTradingSignals signals) // TODO: get rid of whole body lock and make calls async
        {   
            // TODO: check if the Exchange is ready for processing signals, maybe put them into inner queue if readyness is not the case
            
            // TODO: this method should place signals into inner queue only
            // the queue have to be processed via separate worker

            var instrumentName = signals.Instrument.Name;
            
            lock (lockRoots[instrumentName])
            {
                if (!ActualSignals.ContainsKey(instrumentName))
                {
                    LykkeLog.WriteWarningAsync(
                        component: nameof(TradingBot.Exchanges.Abstractions),
                        process: nameof(Exchange),
                        context: nameof(HandleTradingSignals),
                        info:
                        $"ActualSignals doesn't contains a key {instrumentName}. It has keys: {string.Join(", ", ActualSignals.Keys)}"
                    ).Wait();
                    
                    return Task.FromResult(0);
                }
                
                foreach (var arrivedSignal in signals.TradingSignals)
                {
                    var translatedSignal = new TranslatedSignalTableEntity(SignalSource.RabbitQueue, signals.Instrument.Exchange, instrumentName, arrivedSignal);

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
                                        LykkeLog.WriteInfoAsync(nameof(TradingBot.Exchanges.Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            $"Skipping old signal {arrivedSignal}").Wait();
                                        
                                        translatedSignal.Failure("The signal is too old");
                                        break;
                                    }
                                    
                                    existing = ActualSignals[instrumentName]
                                        .SingleOrDefault(x => x.OrderId == arrivedSignal.OrderId);

                                    if (existing != null)
                                        LykkeLog.WriteWarningAsync(nameof(TradingBot.Exchanges.Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            $"An order with id {arrivedSignal.OrderId} already in actual signals.").Wait();


                                    var result = retryTwoTimesPolicy.ExecuteAndCaptureAsync(() =>
                                        AddOrder(signals.Instrument, arrivedSignal, translatedSignal)).Result;

                                    if (result.Outcome == OutcomeType.Successful)
                                    {
                                        ActualSignals[instrumentName].AddLast(arrivedSignal);
                                        
                                        LykkeLog.WriteInfoAsync(nameof(TradingBot.Exchanges.Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            $"Created new order {arrivedSignal}").Wait();
                                    }
                                    else
                                    {
                                        LykkeLog.WriteErrorAsync(nameof(TradingBot.Exchanges.Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            result.FinalException).Wait();
                                        
                                        translatedSignal.Failure(result.FinalException);   
                                    }
                                }
                                catch (Exception e)
                                {
                                    LykkeLog.WriteErrorAsync(nameof(TradingBot.Exchanges.Abstractions),
                                        nameof(Exchange),
                                        nameof(HandleTradingSignals),
                                        e);
                                    translatedSignal.Failure(e);
                                }
                                break;

                            case OrderCommand.Edit:
                                throw new NotSupportedException("Do not support edit signal");

                            case OrderCommand.Cancel:

                                existing = ActualSignals[instrumentName]
                                    .SingleOrDefault(x => x.OrderId == arrivedSignal.OrderId);

                                if (existing == null)
                                {
                                    LykkeLog.WriteWarningAsync(nameof(Abstractions),
                                        nameof(Exchange),
                                        nameof(HandleTradingSignals),
                                        $"Command for cancel unexisted order {arrivedSignal}").Wait();
                                    
                                    translatedSignal.Failure("Unexisted order");
                                    break;
                                }
                                
                                try
                                {
                                    var result = retryTwoTimesPolicy.ExecuteAndCaptureAsync(() =>
                                        CancelOrder(signals.Instrument, existing, translatedSignal)).Result;

                                    if (result.Outcome == OutcomeType.Successful)
                                    {
                                        ActualSignals[instrumentName].Remove(existing);
                                        
                                        LykkeLog.WriteInfoAsync(nameof(Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            $"Canceled order {arrivedSignal}").Wait();
                                    }
                                    else
                                    {
                                        translatedSignal.Failure(result.FinalException);
                                        LykkeLog.WriteErrorAsync(nameof(Abstractions),
                                            nameof(Exchange),
                                            nameof(HandleTradingSignals),
                                            result.FinalException);
                                    }
                                }
                                catch (Exception e)
                                {
                                    translatedSignal.Failure(e);
                                    LykkeLog.WriteErrorAsync(nameof(Abstractions),
                                        nameof(Exchange),
                                        nameof(HandleTradingSignals),
                                        e).Wait();
                                }

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
                    LykkeLog.WriteInfoAsync(nameof(Abstractions),
                        nameof(Exchange),
                        nameof(HandleTradingSignals),
                        $"Current orders:\n {string.Join("\n", ActualSignals[instrumentName])}").Wait();
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
