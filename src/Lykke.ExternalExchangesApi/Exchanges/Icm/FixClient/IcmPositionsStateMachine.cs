using System.Collections.Generic;
using System.Threading;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using QuickFix.Fields;
using QuickFix.FIX44;

namespace Lykke.ExternalExchangesApi.Exchanges.Icm.FixClient
{
    internal sealed class IcmPositionsStateMachine : RequestStateMachine<IReadOnlyList<PositionReport>>
    {
        private readonly List<PositionReport> _positions = new List<PositionReport>();

        public IcmPositionsStateMachine(RequestForPositions positionsRequest, CancellationToken cancellationToken, IMessageHandler messageHandler, ILog log) : base(positionsRequest, cancellationToken, messageHandler, log)
        {
            positionsRequest.PosReqID = new PosReqID(Id);
        }

        public void ProcessResponse(PositionReport message)
        {
            _positions.Add(message);
            var posionsPromissed = message.IsSetTotalNumPosReports() ? message.TotalNumPosReports.Obj : 1;

            if (posionsPromissed == _positions.Count)
            {
                TaskCompletionSource.TrySetResult(_positions);
                Status = RequestStatus.Completed;
            }
        }
    }
}

