using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TradingBot.Infrastructure.Exceptions
{
    public class StatusCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        
        public object Model { get; set; }

        public StatusCodeException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public StatusCodeException(HttpStatusCode statusCode, string message) : base (message)
        {
            StatusCode = statusCode;
        }

        public StatusCodeException(HttpStatusCode statusCode, string message, object model)
            : base (message)
        {
            StatusCode = statusCode;
            Model = model;
        }

        public StatusCodeException(ModelStateDictionary modelState)
            : this (HttpStatusCode.BadRequest, "The model is in invalid state", new SerializableError(modelState))
        {
        }
    }
}