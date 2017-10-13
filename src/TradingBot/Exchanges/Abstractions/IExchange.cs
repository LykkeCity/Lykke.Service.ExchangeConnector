using System;
using System.Collections.Generic;
using System.Threading;
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

        Task<IEnumerable<AccountBalance>> GetAccountBalance(CancellationToken cancellationToken);

        Task<TradeBalanceModel> GetTradeBalance(CancellationToken cancellationToken);
        
        Task<ExecutedTrade> AddOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutedTrade> CancelOrderAndWaitExecution(Instrument instrument, TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<ExecutedTrade> GetOrder(string id, Instrument instrument);

        Task<IEnumerable<ExecutedTrade>> GetOpenOrders(TimeSpan timeout);

        Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout);
    }
}
