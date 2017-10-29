using System;

namespace TradingBot.Infrastructure.Exceptions
{
    public class InsufficientFundsException : ApiException
    {
        public InsufficientFundsException()
        {
        }

        public InsufficientFundsException(string message) : base(message)
        {
        }

        public InsufficientFundsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}