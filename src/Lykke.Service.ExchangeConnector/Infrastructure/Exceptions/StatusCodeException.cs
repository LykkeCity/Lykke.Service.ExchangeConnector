using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Infrastructure.Exceptions
{
    [Serializable]
    public sealed class StatusCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public SerializableError Model { get; set; }

        public StatusCodeException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public StatusCodeException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public StatusCodeException(HttpStatusCode statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
