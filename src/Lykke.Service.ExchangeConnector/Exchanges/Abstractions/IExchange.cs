using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingBot.Models.Api;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Abstractions
{
    public interface IExchange
    {
        string Name { get; }

        ExchangeState State { get; }

        IReadOnlyList<Instrument> Instruments { get; }

        Task<IEnumerable<AccountBalance>> GetAccountBalance(TimeSpan timeout);

        Task<IReadOnlyCollection<TradeBalanceModel>> GetTradeBalances(TimeSpan timeout);
        
        Task<ExecutionReport> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutionReport> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutionReport> GetOrder(string id, Instrument instrument, TimeSpan timeout);

        Task<IEnumerable<ExecutionReport>> GetOpenOrders(TimeSpan timeout);

        Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout);
    }
}
