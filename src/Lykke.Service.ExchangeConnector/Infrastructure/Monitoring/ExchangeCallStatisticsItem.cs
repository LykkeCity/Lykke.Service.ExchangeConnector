using System;

namespace TradingBot.Infrastructure.Monitoring
{
    internal sealed class ExchangeCallStatisticsItem
    {
        public string ExchangeName { get; }
        public string MethodName { get; }
        public TimeSpan Duration { get; }
        public DateTime TimeStamp { get; }

        public ExchangeCallStatisticsItem(string exchangeName, string methodName, TimeSpan duration, DateTime timeStamp)
        {
            ExchangeName = exchangeName;
            MethodName = methodName;
            Duration = duration;
            TimeStamp = timeStamp;
        }
    }
}