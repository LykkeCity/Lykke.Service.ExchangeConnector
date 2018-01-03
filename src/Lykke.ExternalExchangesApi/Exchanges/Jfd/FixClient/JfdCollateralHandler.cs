using System.Threading;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace Lykke.ExternalExchangesApi.Exchanges.Jfd.FixClient
{
    internal sealed class JfdCollateralHandler : MessageHandlerBase<JfdCollateralInquiryStateMachine>
    {

        public JfdCollateralHandler(ILog log, string exchangeName) : base(log, exchangeName)
        {
        }

        public JfdCollateralInquiryStateMachine RegisterMessage(CollateralInquiry request, CancellationToken cancellationToken)
        {
            var or = new JfdCollateralInquiryStateMachine(request, cancellationToken, this, Log);
            Requests[or.Id] = or;
            return or;
        }

        public override bool HandleMessage(Message message)
        {
            var ack = message as CollateralInquiryAck;
            var pr = message as CollateralReport;
            if (ack == null && pr == null)
            {
                return false;
            }

            var id = ack?.CollInquiryID.Obj ?? pr?.CollInquiryID.Obj;

            if (!Requests.TryGetValue(id, out var request))
            {
                Log.WriteWarningAsync(nameof(HandleMessage), "Handle collateral report from Jfd", $"Received response with unknown id {id}").GetAwaiter().GetResult();
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
