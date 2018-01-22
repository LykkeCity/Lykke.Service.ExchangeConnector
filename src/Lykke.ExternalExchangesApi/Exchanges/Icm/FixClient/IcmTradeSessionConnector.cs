using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.FIX44;
using ILog = Common.Log.ILog;

namespace Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient
{
    public sealed class IcmTradeSessionConnector : FixTradeSessionConnector
    {
        private const string ExchangeName = "ICM";
        private readonly OrdersHandler _ordersHandler;
        private readonly IcmPositionsHandler _jfdPositionsHandler;

        public IcmTradeSessionConnector(FixConnectorConfiguration config, ILog log) : base(config, log)
        {

            _ordersHandler = new OrdersHandler(log, ExchangeName);
            _jfdPositionsHandler = new IcmPositionsHandler(log, ExchangeName);
            Handlers.Add(_ordersHandler);
            Handlers.Add(_jfdPositionsHandler);

        }

        public Task<ExecutionReport> AddOrderAsync(NewOrderSingle order, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            lock (RejectLock)
            {
                var request = _ordersHandler.RegisterMessage(order, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }

        public Task<IReadOnlyList<PositionReport>> GetPositionsAsync(RequestForPositions positionRequest, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();
            lock (RejectLock)
            {
                var request = _jfdPositionsHandler.RegisterMessage(positionRequest, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }

    }
}
