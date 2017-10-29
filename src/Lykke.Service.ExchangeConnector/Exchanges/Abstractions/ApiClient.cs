using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradingBot.Infrastructure.Exceptions;
using System.IO;
using System;
using System.Threading;
using Common.Log;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Abstractions
{
    public class ApiClient
    {
        private readonly ILog lykkeLog;
        
        private readonly HttpClient httpClient;

        public ApiClient(HttpClient httpClient, ILog lykkeLog)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.lykkeLog = lykkeLog;
        }
        
        public async Task<TResponse> MakeGetRequestAsync<TResponse>(string url, CancellationToken cancellationToken)
        {
            Log(nameof(MakeGetRequestAsync), $"Making request to url: {url}");

            cancellationToken.ThrowIfCancellationRequested();
            using (var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException($"Unexpected status code: {response.StatusCode}. {content}");
                }

                Log(nameof(MakeGetRequestAsync), $"Received content: {content}");

                var result = JsonConvert.DeserializeObject<TResponse>(content);
                return result;
            }
        }

        public async Task<TResponse> MakePostRequestAsync<TResponse>(string url, HttpContent content, 
            TranslatedSignalTableEntity translatedSignal, 
            CancellationToken cancellationToken)
        {
            translatedSignal?.RequestSent(HttpMethod.Post, url, content);
            Log(nameof(MakePostRequestAsync), $"Making request to url: {url}. {translatedSignal?.RequestSentToExchange}");
            
            cancellationToken.ThrowIfCancellationRequested();
            using (var response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                translatedSignal?.ResponseReceived(responseContent);
                Log(nameof(MakePostRequestAsync), $"Received content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException($"Unsuccess status code: {response.StatusCode}. {responseContent}");
                }

                try
                {
                    return JsonConvert.DeserializeObject<TResponse>(responseContent);
                }
                catch (Exception e)
                {
                    throw new ApiException($"Can't deserialize response to type {typeof(TResponse)}", e);
                }
            }
        }

        public Task<Stream> MakeStreamRequestAsync(string url)
        {
            return httpClient.GetStreamAsync(url);
        }

        private void Log(string context, string message)
        {
            const int maxMessageLen = 32000;
            
            if (message.Length >= maxMessageLen)
                message = message.Substring(0, maxMessageLen);
                
            lykkeLog.WriteInfoAsync(
                nameof(ApiClient),
                nameof(ApiClient),
                context,
                message);
        }
    }
}
