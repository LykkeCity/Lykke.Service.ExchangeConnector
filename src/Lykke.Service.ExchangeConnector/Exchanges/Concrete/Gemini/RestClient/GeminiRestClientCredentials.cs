using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using TradingBot.Exchanges.Concrete.Gemini.Credentials;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient
{
    internal sealed class GeminiRestClientCredentials : ServiceClientCredentials
    {
        private const string _accessKeyHeader = "X-GEMINI-APIKEY";
        private const string _accessPayload = "X-GEMINI-PAYLOAD";
        private const string _accessSignatureHeader = "X-GEMINI-SIGNATURE";

        private readonly GeminiCredentialsFactory _credentialsFactory;

        public GeminiRestClientCredentials(string apiKey, string apiSecret)
        {
            _credentialsFactory = new GeminiCredentialsFactory(apiKey, apiSecret);
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = await GetContent(request, cancellationToken);
            
            var credentials = _credentialsFactory.GenerateCredentials(request.Method, request.RequestUri, content);

            request.Headers.Add(_accessKeyHeader, credentials.ApiKey);
            request.Headers.Add(_accessSignatureHeader, credentials.Signature);
            request.Headers.Add(_accessPayload, credentials.PayLoad);
        }

        private static async Task<string> GetContent(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ReadAsStringAsync() doesn't have an overaload accepting cancellationToken, that's why we are forcing the 
            // operation to be cancelled, even though the method will continue working in background until it finishes execution
            return request.Content == null 
                ? string.Empty 
                : await request.Content.ReadAsStringAsync()   
                    .WithCancellation(cancellationToken);    
        }
    }
}
