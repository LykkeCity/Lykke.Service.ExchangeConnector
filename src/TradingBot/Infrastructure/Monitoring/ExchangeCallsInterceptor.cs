using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot.Infrastructure.Monitoring
{
    internal sealed class ExchangeCallsInterceptor : IInterceptor
    {
        private readonly ExchangeStatisticsCollector _statisticsCollector;


        public ExchangeCallsInterceptor(ExchangeStatisticsCollector statisticsCollector)
        {
            _statisticsCollector = statisticsCollector;
        }


        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            var method = invocation.MethodInvocationTarget;
            if (invocation.ReturnValue is Task && method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, invocation);
            }
        }

        private static Task InterceptAsync(Task task, IInvocation invocation)
        {
            return task;
        }

        private async Task<T> InterceptAsync<T>(Task<T> task, IInvocation invocation)
        {
            var sw = new Stopwatch();
            sw.Start();
            var ex = invocation.InvocationTarget as Exchange;
            try
            {
                var result = await task.ConfigureAwait(false);
                if (ex != null)
                {
                    _statisticsCollector.RegisterMethodCall(ex, invocation.Method.Name, sw.Elapsed);
                }
                return result;
            }
            catch (Exception e)
            {
                _statisticsCollector.RegisterException(ex, invocation.Method.Name, e);

                throw;
            }

        }
    }
}
