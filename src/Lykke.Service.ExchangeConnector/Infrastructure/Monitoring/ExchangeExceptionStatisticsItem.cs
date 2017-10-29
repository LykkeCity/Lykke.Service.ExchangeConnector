using System;

namespace TradingBot.Infrastructure.Monitoring
{
    internal sealed class ExchangeExceptionStatisticsItem
    {
        public string ExchangeName { get; }
        public string MethodName { get; }
        public string Message { get; }
        public DateTime TimeStamp { get; }

        public ExchangeExceptionStatisticsItem(string exchangeName, string methodName, string message, DateTime timeStamp)
        {
            ExchangeName = exchangeName;
            MethodName = methodName;
            Message = message;
            TimeStamp = timeStamp;
        }
    }
}