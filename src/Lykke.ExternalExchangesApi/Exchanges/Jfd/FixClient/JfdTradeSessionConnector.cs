using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.FIX44;
using ILog = Common.Log.ILog;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    public sealed class JfdTradeSessionConnector : FixTradeSessionConnector
    {
        private const string ExchangeName = "Jfd";
        private readonly OrdersHandler _ordersHandler;
        private readonly JfdPositionsHandler _jfdPositionsHandler;
        private readonly JfdCollateralHandler _jfdCollateralHandler;

        public JfdTradeSessionConnector(FixConnectorConfiguration config, ILog log) : base(config, log)
        {

            _ordersHandler = new OrdersHandler(log, ExchangeName);
            _jfdPositionsHandler = new JfdPositionsHandler(log, ExchangeName);
            _jfdCollateralHandler = new JfdCollateralHandler(log, ExchangeName);
            Handlers.Add(_ordersHandler);
            Handlers.Add(_jfdPositionsHandler);
            Handlers.Add(_jfdCollateralHandler);

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

        public Task<IReadOnlyCollection<PositionReport>> GetPositionsAsync(RequestForPositions positionRequest, CancellationToken cancellationToken)
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

        public Task<IReadOnlyCollection<CollateralReport>> GetCollateralAsync(CollateralInquiry collateralInquiry, CancellationToken cancellationToken)
        {
            EnsureCanHandleRequest();

            lock (RejectLock)
            {
                var request = _jfdCollateralHandler.RegisterMessage(collateralInquiry, cancellationToken);
                SendRequest(request.Message);
                RegisterForRejectResponse(request);
                var result = request.Send();
                return result;
            }

        }
    }
}
