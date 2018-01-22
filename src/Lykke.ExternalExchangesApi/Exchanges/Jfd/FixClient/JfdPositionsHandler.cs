using System.Threading;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    internal sealed class JfdPositionsHandler : MessageHandlerBase<JfdPositionsStateMachine>
    {
        public JfdPositionsHandler(ILog log, string exchangeName) : base(log, exchangeName)
        {

        }

        public JfdPositionsStateMachine RegisterMessage(RequestForPositions request, CancellationToken cancellationToken)
        {
            var or = new JfdPositionsStateMachine(request, cancellationToken, this, Log);
            Requests[or.Id] = or;
            return or;
        }

        public override bool HandleMessage(Message message)
        {
            var ack = message as RequestForPositionsAck;
            var pr = message as PositionReport;
            if (ack == null && pr == null)
            {
                return false;
            }

            var id = ack?.PosReqID.Obj ?? pr?.PosReqID.Obj;

            if (!Requests.TryGetValue(id, out var request))
            {
                Log.WriteWarningAsync(nameof(HandleMessage), "Handle positions report from Jfd", $"Received response with unknown id {id}").GetAwaiter().GetResult();
                return false;
            }
            if (ack != null)
            {
                request.ProcessResponse(ack);
            }
            else
            {
                request.ProcessResponse(pr);
            }
            if (request.Status == RequestStatus.Completed)
            {
                Requests.Remove(request.Id);
            }
            return true;
        }
    }
}
