using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common;
using TradingBot.Repositories;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Icm
{
    internal interface IIcmConnector : IStartable, IStopable
    {
        Task<ExecutionReport> AddOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);
        Task<ExecutionReport> CancelOrderAndWaitResponse(TradingSignal signal, TranslatedSignalTableEntity translatedSignal, TimeSpan timeout);
        Task<ExecutionReport> GetOrderInfoAndWaitResponse(Instrument instrument, string orderId);
        Task<IEnumerable<ExecutionReport>> GetAllOrdersInfo(TimeSpan timeout);
        event Action Connected;
        event Action Disconnected;
    }
}
