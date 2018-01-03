using System.Threading;
using Common.Log;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Shared
{
    internal sealed class OrdersHandler : MessageHandlerBase<OrderStateMachine>
    {
        public OrdersHandler(ILog log, string exchangeName) : base(log, exchangeName)
        {
        }

        public OrderStateMachine RegisterMessage(NewOrderSingle newOrderSingle, CancellationToken cancellationToken)
        {
            var or = new OrderStateMachine(newOrderSingle, cancellationToken, this, Log);
            Requests[or.Id] = or;
            return or;
        }

        public override bool HandleMessage(Message message)
        {
            if (!(message is ExecutionReport or))
            {
                return false;
            }
            var id = or.ClOrdID.Obj;
            if (!Requests.TryGetValue(id, out var request))
            {
                Log.WriteWarningAsync(nameof(HandleMessage), "Handle execution report from Jfd", $"Received response with unknown id {id}");
                return false;
            }
            request.ProcessResponse(or);
            if (request.Status == RequestStatus.Completed)
            {
                Requests.Remove(request.Id);
            }
            return true;
        }

        public bool HandleReject(Message message)
        {
            throw new System.NotImplementedException();
        }

        public bool RegisterMessage(Message message)
        {
            throw new System.NotImplementedException();
        }
    }
}
