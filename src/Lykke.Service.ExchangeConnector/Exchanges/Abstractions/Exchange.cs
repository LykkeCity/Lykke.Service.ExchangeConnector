using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Common.Log;
using TradingBot.Communications;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Models.Api;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Abstractions
{
    internal abstract class Exchange : IExchange
    {
        protected readonly ILog LykkeLog;

        public string Name { get; }

        internal IExchangeConfiguration Config { get; }

        public ExchangeState State { get; private set; }

        public IReadOnlyList<Instrument> Instruments { get; }

        protected Exchange(string name, IExchangeConfiguration config, 
            TranslatedSignalsRepository translatedSignalsRepository, ILog log)
        {
            Name = name;
            Config = config;
            State = ExchangeState.Initializing;
            LykkeLog = log;

            if (config.SupportedCurrencySymbols == null || 
                config.SupportedCurrencySymbols.Count == 0)
            {
                throw new ArgumentException($"There is no instruments in the settings for {Name} exchange");
            }

            Instruments = config.SupportedCurrencySymbols
                .Select(x => new Instrument(Name, x.LykkeSymbol)).ToList();
        }

        public void Start()
        {
            LykkeLog.WriteInfoAsync(nameof(Exchange), nameof(Start), Name, $"Starting exchange {Name}, current state is {State}").Wait();

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

        public abstract Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout);

        public abstract Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal,
            TranslatedSignalTableEntity translatedSignal,
            TimeSpan timeout);

        public virtual Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout)
        {
            throw new NotSupportedException($"{Name} does not support receiving order information by {nameof(id)} and {nameof(instrument)}");
        }

        public virtual Task<IEnumerable<AccountBalance>> GetAccountBalance(TimeSpan timeout)
        {
            return Task.FromResult(Enumerable.Empty<AccountBalance>());
        }

        public virtual Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public virtual Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public virtual Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }
    }
}
