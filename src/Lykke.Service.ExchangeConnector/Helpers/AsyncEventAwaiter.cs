using System.Threading;
using System.Threading.Tasks;

namespace TradingBot.Helpers
{
    internal class AsyncEventAwaiter<T>
    {
        private AsyncEvent<T> _asyncEvent;
        private readonly TaskCompletionSource<T> _completionSource;

        public AsyncEventAwaiter(ref AsyncEvent<T> asyncEvent)
        {
            asyncEvent += SubscriptionHandler;
            _asyncEvent = asyncEvent;
            _completionSource = new TaskCompletionSource<T>();
        }

        public async Task<T> Await(CancellationToken cancellationToken)
        {
            try
            {
                await _completionSource.Task
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _asyncEvent -= SubscriptionHandler;
            }

            return _completionSource.Task.Result;
        }

        private Task SubscriptionHandler(object sender, T message)
        {
            _completionSource.SetResult(message);
            return _completionSource.Task;
        }
    }
}
