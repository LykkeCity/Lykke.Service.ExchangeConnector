using System;

namespace TradingBot.Infrastructure.Exceptions
{
    internal sealed class OperationRejectedException : Exception
    {
        public OperationRejectedException(string message) : base(message)
        {
            
        }
    }
}
