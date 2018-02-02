using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.ExchangeDataStore.Core.Domain.Events
{
    /// <summary>
    /// Asynchronous event handler delegate.
    /// Source: https://stackoverflow.com/a/30739162
    /// </summary>
    public class AsyncEvent<TEventArgs> 
    {
        private readonly List<Func<object, TEventArgs, Task>> _invocationList;
        private readonly object _locker;

        private AsyncEvent()
        {
            _invocationList = new List<Func<object, TEventArgs, Task>>();
            _locker = new object();
        }

        public static AsyncEvent<TEventArgs> operator +(
            AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            //Note: Thread safety issue- if two threads register to the same event (on the first time, i.e when it is null)
            //they could get a different instance, so whoever was first will be overridden.
            //A solution for that would be to switch to a public constructor and use it, but then we'll 'lose' the similar syntax to c# events             
            if (e == null)
                e = new AsyncEvent<TEventArgs>();

            lock (e._locker)
            {
                e._invocationList.Add(callback);
            }
            return e;
        }

        public static AsyncEvent<TEventArgs> operator -(
            AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            if (e == null)
                return null;

            lock (e._locker)
            {
                e._invocationList.Remove(callback);
            }
            return e;
        }

        public async Task InvokeAsync(object sender, TEventArgs eventArgs)
        {
            List<Func<object, TEventArgs, Task>> tmpInvocationList;
            lock (_locker)
            {
                tmpInvocationList = new List<Func<object, TEventArgs, Task>>(_invocationList);
            }

            foreach (var callback in tmpInvocationList)
            {
                //Assuming we want a serial invocation, for a parallel invocation we can use Task.WhenAll instead
                await callback(sender, eventArgs);
            }
        }
    }
}
