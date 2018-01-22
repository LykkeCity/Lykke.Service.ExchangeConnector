using System.Threading;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient
{
    internal sealed class IcmPositionsHandler : MessageHandlerBase<IcmPositionsStateMachine>
    {
        public IcmPositionsHandler(ILog log, string exchangeName) : base(log, exchangeName)
        {

        }

        public IcmPositionsStateMachine RegisterMessage(RequestForPositions request, CancellationToken cancellationToken)
        {
            var or = new IcmPositionsStateMachine(request, cancellationToken, this, Log);
            Requests[or.Id] = or;
            return or;
        }

        public override bool HandleMessage(Message message)
        {
            if (!(message is PositionReport pr))
            {
                return false;
            }

            var id = pr.PosReqID.Obj;

            if (!Requests.TryGetValue(id, out var request))
            {
                Log.WriteWarningAsync(nameof(HandleMessage), $"Handle positions report from {ExchangeName}", $"Received response with unknown id {id}").GetAwaiter().GetResult();
                return false;
            }

            request.ProcessResponse(pr);
            if (request.Status == RequestStatus.Completed)
            {
                Requests.Remove(request.Id);
            }
            return true;
        }
    }
}
