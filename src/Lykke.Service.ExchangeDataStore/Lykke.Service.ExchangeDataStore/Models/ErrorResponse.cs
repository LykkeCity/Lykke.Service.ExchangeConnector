using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.ExchangeDataStore.Models
{
    public class ErrorResponse
    {
        public string ErrorMessage { get; }

        public Dictionary<string, List<string>> ModelErrors { get; }

        private ErrorResponse() :
            this(null)
        {
        }

        private ErrorResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            ModelErrors = new Dictionary<string, List<string>>();
        }

        public ErrorResponse AddModelError(string key, string message)
        {
            if (!ModelErrors.TryGetValue(key, out List<string> errors))
            {
                errors = new List<string>();

                ModelErrors.Add(key, errors);
            }

            errors.Add(message);

            return this;
        }

        public ErrorResponse AddModelError(string key, Exception exception)
        {
            var ex = exception;
            var sb = new StringBuilder();

            while (true)
            {
                sb.AppendLine(ex.Message);

                ex = ex.InnerException;

                if (ex == null)
                {
                    return AddModelError(key, sb.ToString());
                }

                sb.Append(" -> ");
            }
        }

        public static ErrorResponse Create()
        {
            return new ErrorResponse();
        }

        public static ErrorResponse Create(ModelStateDictionary modelState)
        {
            var response = new ErrorResponse();

            foreach (var state in modelState)
            {
                var messages = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList();

                response.ModelErrors.Add(state.Key, messages);
            }

            return response;
        }

        public static ErrorResponse Create(string message)
        {
            return new ErrorResponse(message);
        }
    }
}