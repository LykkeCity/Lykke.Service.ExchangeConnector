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
        
        Task<OrderStatusUpdate> AddOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<OrderStatusUpdate> CancelOrderAndWaitExecution(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);

        Task<OrderStatusUpdate> GetOrder(string id, Instrument instrument, TimeSpan timeout);

        Task<IEnumerable<OrderStatusUpdate>> GetOpenOrders(TimeSpan timeout);

        Task<IReadOnlyCollection<PositionModel>> GetPositions(TimeSpan timeout);
    }
}
