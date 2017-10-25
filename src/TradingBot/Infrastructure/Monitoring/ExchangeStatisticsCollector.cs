using System;
using System.Collections.Generic;
using System.Linq;
using TradingBot.Exchanges.Abstractions;

namespace TradingBot.Infrastructure.Monitoring
{
    internal sealed class ExchangeStatisticsCollector
    {
        private const int HistoryDepth = 100;
        private readonly HashSet<string> _monitoringMethodsNames;

        public ExchangeStatisticsCollector()
        {
            // ReSharper disable once LocalNameCapturedOnly
            Exchange ex;
            _monitoringMethodsNames = new HashSet<string>
            {
                nameof(ex.GetOpenOrders),
                nameof(ex.GetTradeBalances),
                nameof(ex.AddOrderAndWaitExecution),
                nameof(ex.CancelOrderAndWaitExecution),
                nameof(ex.GetOrder),
                nameof(ex.GetPositions)
            };
        }

        private readonly CircularBuffer<ExchangeCallStatisticsItem> _callStatistics = new CircularBuffer<ExchangeCallStatisticsItem>(HistoryDepth);
        private readonly CircularBuffer<ExchangeExceptionStatisticsItem> _exceptionStatistics = new CircularBuffer<ExchangeExceptionStatisticsItem>(HistoryDepth);

        public IReadOnlyCollection<ExchangeCallStatisticsItem> GetCallStatistics()
        {
            return _callStatistics.Buffer.Where(v => v != null).ToArray();
        }

        public IReadOnlyCollection<ExchangeExceptionStatisticsItem> GetExceptionStatistics()
        {
            return _exceptionStatistics.Buffer.Where(v => v != null).ToArray();
        }


        public void RegisterMethodCall(Exchange exchange, string methodName, TimeSpan duration)
        {
            if (_monitoringMethodsNames.Contains(methodName))
            {
                _callStatistics.Add(new ExchangeCallStatisticsItem(exchange.Name, methodName, duration, DateTime.UtcNow));
            }
        }

        public void RegisterException(Exchange exchange, string methodName, Exception exception)
        {
            if (_monitoringMethodsNames.Contains(methodName))
            {
                _exceptionStatistics.Add(new ExchangeExceptionStatisticsItem(exchange.Name, methodName, exception.Message, DateTime.UtcNow));
            }
        }

        private class CircularBuffer<T>
        {
            private readonly T[] _buffer;
            private int _nextFree;


            public CircularBuffer(int length)
            {
                _buffer = new T[length];
                _nextFree = 0;
            }

            public void Add(T o)
            {

                _buffer[_nextFree] = o;
                _nextFree = (_nextFree + 1) % _buffer.Length;
            }

            public IReadOnlyCollection<T> Buffer => _buffer;
        }
    }
}
