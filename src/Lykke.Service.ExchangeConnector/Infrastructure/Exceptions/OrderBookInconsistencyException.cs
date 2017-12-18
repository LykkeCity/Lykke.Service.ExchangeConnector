using System;
using Lykke.ExternalExchangesApi.Exceptions;

namespace TradingBot.Infrastructure.Exceptions
{
    internal class OrderBookInconsistencyException : ApiException
    {
        public OrderBookInconsistencyException(string message) : base(message)
        {
        }

        public OrderBookInconsistencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
