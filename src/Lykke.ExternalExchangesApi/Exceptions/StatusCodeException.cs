using System;
using System.Collections.Generic;
using System.Net;

namespace Lykke.ExternalExchangesApi.Exceptions
{
    [Serializable]
    public sealed class StatusCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public IDictionary<string, object> Model { get; set; }

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
