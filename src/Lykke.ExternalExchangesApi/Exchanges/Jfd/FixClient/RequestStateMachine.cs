using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using QuickFix;
using ILog = Common.Log.ILog;

namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal abstract class RequestStateMachine<T> : IRequest
    {
        private readonly IMessageHandler _messageHandler;
        public CancellationToken CancellationToken { get; }
        public RequestStatus Status { get; protected set; }
        public readonly string Id;

        public Message Message { get; }

        protected readonly ILog Log;
        protected readonly TaskCompletionSource<T> TaskCompletionSource = new TaskCompletionSource<T>();


        protected RequestStateMachine(QuickFix.Message message, CancellationToken cancellationToken, IMessageHandler messageHandler, ILog log)
        {
            Message = message;
            _messageHandler = messageHandler;
            CancellationToken = cancellationToken;
            Id = message.GetType().Name + Guid.NewGuid();
            Log = Log = log.CreateComponentScope(GetType().Name);
            cancellationToken.Register(() => TaskCompletionSource.TrySetCanceled(cancellationToken));
            if (cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource.TrySetCanceled(cancellationToken);
                Status = RequestStatus.Completed;
            }
        }

        public void Reject(string reason)
        {
            Status = RequestStatus.Completed;
            TaskCompletionSource.TrySetException(new InvalidOperationException(reason));
            _messageHandler.RejectMessage(Id);

        }

        public Task<T> Send()
        {
            Status = RequestStatus.Sent;
            return TaskCompletionSource.Task;
        }
    }
}
