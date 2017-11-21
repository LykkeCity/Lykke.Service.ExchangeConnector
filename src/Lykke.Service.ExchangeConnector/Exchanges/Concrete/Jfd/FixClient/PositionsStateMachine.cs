using System;
using System.Collections.Generic;
using System.Threading;
using Common.Log;
using QuickFix.Fields;
using QuickFix.FIX44;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal sealed class PositionsStateMachine : RequestStateMachine<IReadOnlyCollection<PositionReport>>
    {
        private bool _ackReceived;
        private int _promisedReports;
        private readonly List<PositionReport> _positions = new List<PositionReport>();


        public PositionsStateMachine(RequestForPositions positionsRequest, CancellationToken cancellationToken, IMessageHandler messageHandler, ILog log) : base(positionsRequest, cancellationToken, messageHandler, log)
        {
            positionsRequest.PosReqID = new PosReqID(Id);
        }

        public void ProcessResponse(RequestForPositionsAck message)
        {
            if (_ackReceived)
            {
                Log.WriteWarningAsync(nameof(ProcessResponse), "Handling response from Jfd", $"Unexpected RequestForPositionsAck received. Id {message.PosReqID.Obj}").GetAwaiter().GetResult();
            }
            _ackReceived = true;

            if (message.PosReqResult.Obj == PosReqResult.INVALID_OR_UNSUPPORTED_REQUEST)
            {
                var msg = message.IsSetText() ? message.Text.Obj : "Position request rejected. No additional information";
                TaskCompletionSource.SetException(new OperationRejectedException(msg));
                Status = RequestStatus.Completed;
            }
            _promisedReports = message.TotalNumPosReports.Obj;
            if (_promisedReports == 0)
            {
                TaskCompletionSource.TrySetResult(Array.Empty<PositionReport>());
                Status = RequestStatus.Completed;
            }
            else
            {
                Status = RequestStatus.InProgress;
            }
        }

        public void ProcessResponse(PositionReport message)
        {
            _positions.Add(message);
            if (_positions.Count == _promisedReports)
            {
                TaskCompletionSource.TrySetResult(_positions);
                Status = RequestStatus.Completed;
            }
        }
    }
}
