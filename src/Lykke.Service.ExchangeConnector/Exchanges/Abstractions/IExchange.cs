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
        
        Task<ExecutedTrade> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutedTrade> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutedTrade> GetOrder(string id, Instrument instrument, TimeSpan timeout);

        Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout);

        Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout);
    }
}
