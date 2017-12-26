using System;
using System.Threading;
using QuickFix.Fields;
using QuickFix.FIX44;
using ILog = Common.Log.ILog;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal sealed class OrderStateMachine : RequestStateMachine<ExecutionReport>
    {
        public OrderStateMachine(NewOrderSingle newOrderSingle, CancellationToken cancellationToken, IMessageHandler messageHandler, ILog log) : base(newOrderSingle, cancellationToken, messageHandler, log)
        {
            newOrderSingle.ClOrdID = new ClOrdID(Id);
        }

        public void ProcessResponse(ExecutionReport message)
        {
            if (!(Status == RequestStatus.InProgress || Status == RequestStatus.Sent))
            {
                return;
            }
            try
            {
                switch (message.OrdStatus.Obj)
                {
                    case OrdStatus.FILLED:
                    case OrdStatus.CANCELED:
                        TaskCompletionSource.TrySetResult(message);
                        Status = RequestStatus.Completed;
                        break;
                    case OrdStatus.REJECTED:
                        TaskCompletionSource.TrySetException(new InvalidOperationException(message.Text.Obj));
                        Status = RequestStatus.Completed;
                        break;
                    case OrdStatus.PARTIALLY_FILLED:
                    case OrdStatus.PENDING_NEW:
                        Status = RequestStatus.InProgress;
                        break;
                    default:
                        Log.WriteWarningAsync(nameof(ProcessResponse), "Handling response from Jfd", $"Unknown order status {message.OrdStatus.Obj}").GetAwaiter().GetResult();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorAsync(nameof(ProcessResponse), "Handling response from Jfd", ex);
                TaskCompletionSource.TrySetException(ex);
                Status = RequestStatus.Completed;
            }

        }


    }
}
