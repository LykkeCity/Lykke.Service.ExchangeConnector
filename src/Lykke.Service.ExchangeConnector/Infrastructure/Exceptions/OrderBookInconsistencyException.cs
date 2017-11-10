using System;

namespace TradingBot.Infrastructure.Exceptions
{
    public class OrderBookInconsistencyException : ApiException
    {
        public OrderBookInconsistencyException(string message) : base(message)
        {
        }

        public OrderBookInconsistencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
