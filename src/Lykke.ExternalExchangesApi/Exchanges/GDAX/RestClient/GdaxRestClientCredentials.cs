using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.Credentials;
using Lykke.ExternalExchangesApi.Helpers;
using Microsoft.Rest;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient
{
    internal sealed class GdaxRestClientCredentials : ServiceClientCredentials
    {
        private const string _accessKeyHeader = "CB-ACCESS-KEY";
        private const string _accessSignatureHeader = "CB-ACCESS-SIGN";
        private const string _timeStampHeader = "CB-ACCESS-TIMESTAMP";
        private const string _passPhraseHeader = "CB-ACCESS-PASSPHRASE";

        private readonly GdaxCredentialsFactory _credentialsFactory;

        public GdaxRestClientCredentials(string apiKey, string apiSecret, string passPhrase)
        {
            _credentialsFactory = new GdaxCredentialsFactory(apiKey, apiSecret, passPhrase);
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
