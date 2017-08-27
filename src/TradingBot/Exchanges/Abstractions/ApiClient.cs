using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradingBot.Infrastructure.Exceptions;
using System.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using TradingBot.Infrastructure.Logging;

namespace TradingBot.Exchanges.Abstractions
{
    public class ApiClient
    {
        private readonly ILogger logger = Logging.CreateLogger<ApiClient>();
        
        private readonly HttpClient httpClient;

        public ApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
        
        public async Task<TResponse> MakeGetRequestAsync<TResponse>(string url, CancellationToken cancellationToken)
        {
            Log($"Making request to url: {url}");

            using (var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException($"Unexpected status code: {response.StatusCode}. {content}");
                }

                Log($"Received content: {content}");

                var result = JsonConvert.DeserializeObject<TResponse>(content);
                return result;
            }
        }

        public async Task<TResponse> MakePostRequestAsync<TResponse>(string url, HttpContent content, CancellationToken cancellationToken)
        {
            using (var response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {                    
                    throw new ApiException($"Unexpected status code: {response.StatusCode}. {responseContent}");
                }

                Log($"Received content: {responseContent}");

                var result = JsonConvert.DeserializeObject<TResponse>(responseContent);                
                return result;
            }
        }

        public Task<Stream> MakeStreamRequestAsync(string url)
        {
            return httpClient.GetStreamAsync(url);
        }

        private void Log(string message)
        {
            logger.LogDebug(message);
        }
    }
}