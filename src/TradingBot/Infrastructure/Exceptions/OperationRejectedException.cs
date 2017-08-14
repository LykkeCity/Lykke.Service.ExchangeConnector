using System;

namespace TradingBot.Infrastructure.Exceptions
{
    public class OperationRejectedException : Exception
    {
        public OperationRejectedException(string message) : base(message)
        {
            
        }
    }
}