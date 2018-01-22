using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace Lykke.Service.ExchangeConnector.Client
{
    internal sealed class ExchangeConnectorClientCredentials : ServiceClientCredentials
    {
        private const string HeaderName = "X-ApiKey";
        private readonly string _apiKey;

        public ExchangeConnectorClientCredentials(string apiKey)
        {
            _apiKey = apiKey;
        }


        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(HeaderName, _apiKey);
            return Task.CompletedTask;
        }
    }
}
