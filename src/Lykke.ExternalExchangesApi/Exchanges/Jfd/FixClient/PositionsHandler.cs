using System.Threading;
using Common.Log;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal sealed class PositionsHandler : MessageHandlerBase<PositionsStateMachine>
    {
        public PositionsHandler(ILog log) : base(log)
        {

        }

        public PositionsStateMachine RegisterMessage(RequestForPositions request, CancellationToken cancellationToken)
        {
            var or = new PositionsStateMachine(request, cancellationToken, this, Log);
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
