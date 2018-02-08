using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global
//ErrorMessage must be public for external access
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Lykke.Service.ExchangeDataStore.Models
{
    public class ErrorResponse
    {
        public string ErrorMessage { get; }

        public Dictionary<string, List<string>> ModelErrors { get; }

        private ErrorResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            ModelErrors = new Dictionary<string, List<string>>();
        }

        public static ErrorResponse Create(string message)
        {
            return new ErrorResponse(message);
        }
    }
}
