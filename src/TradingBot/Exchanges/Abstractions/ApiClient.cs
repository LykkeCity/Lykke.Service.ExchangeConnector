using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradingBot.Infrastructure.Exceptions;
using System.IO;
using Microsoft.Extensions.Logging;
using TradingBot.Infrastructure;
using System;
using System.Threading;

namespace TradingBot.Exchanges.Abstractions
{
    public class ApiClient
    {
        private readonly ILogger Logger = Logging.CreateLogger<ApiClient>();
        
        private readonly HttpClient httpClient;

        public ApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
        
        public async Task<TResponse> MakeGetRequestAsync<TResponse>(string url, CancellationToken cancellationToken)
        {
            Log($"Making request to url: {url}");

            using (var response = await httpClient.GetAsync(url, cancellationToken)) // todo: ConfigureAwait(false) ??
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();

                    throw new ApiException($"Unexpected status code: {response.StatusCode}. {errorContent}");
                }

                string content = await response.Content.ReadAsStringAsync();

                Log($"Received content: {content}");

                var result = JsonConvert.DeserializeObject<TResponse>(content);

                return result;
            }
        }

        public async Task<Stream> MakeStreamRequestAsync(string url)
        {
            return await httpClient.GetStreamAsync(url);
        }

        private void Log(string message)
        {
            Logger.LogDebug(message);
        }
    }
}