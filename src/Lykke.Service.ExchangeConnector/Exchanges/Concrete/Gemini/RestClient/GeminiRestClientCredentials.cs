using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using TradingBot.Exchanges.Concrete.Gemini.Credentials;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient
{
    internal sealed class GeminiRestClientCredentials : ServiceClientCredentials
    {
        private const string _accessKeyHeader = "CB-ACCESS-KEY";
        private const string _accessSignatureHeader = "CB-ACCESS-SIGN";
        private const string _timeStampHeader = "CB-ACCESS-TIMESTAMP";
        private const string _passPhraseHeader = "CB-ACCESS-PASSPHRASE";

        private readonly GeminiCredentialsFactory _credentialsFactory;

        public GeminiRestClientCredentials(string apiKey, string apiSecret, string passPhrase)
        {
            _credentialsFactory = new GeminiCredentialsFactory(apiKey, apiSecret, passPhrase);
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = await GetContent(request, cancellationToken);
            
            var credentials = _credentialsFactory.GenerateCredentials(request.Method, request.RequestUri, content);

            request.Headers.Add(_accessKeyHeader, credentials.ApiKey);
            request.Headers.Add(_accessSignatureHeader, credentials.Signature);
            request.Headers.Add(_timeStampHeader, credentials.UnixTimestampString);
            request.Headers.Add(_passPhraseHeader, credentials.PassPhrase);
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
