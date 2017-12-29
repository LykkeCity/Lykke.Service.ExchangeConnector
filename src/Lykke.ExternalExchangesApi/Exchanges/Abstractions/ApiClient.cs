using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Exceptions;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Abstractions
{
    public class ApiClient
    {
        private readonly ILog _lykkeLog;
        
        private readonly HttpClient _httpClient;

        public ApiClient(HttpClient httpClient, ILog lykkeLog)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._lykkeLog = lykkeLog;
        }
        
        public async Task<TResponse> MakeGetRequestAsync<TResponse>(string url, CancellationToken cancellationToken)
        {
            Log(nameof(MakeGetRequestAsync), $"Making request to url: {url}");

            cancellationToken.ThrowIfCancellationRequested();
            using (var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
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
            Func<HttpMethod, string, HttpContent, string> requestSent, Action<string> responseReceived,
            CancellationToken cancellationToken)
        {
            var sentContent = requestSent?.Invoke(HttpMethod.Post, url, content);
            Log(nameof(MakePostRequestAsync), $"Making request to url: {url}. {sentContent}");
            
            cancellationToken.ThrowIfCancellationRequested();
            using (var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
            {
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                responseReceived?.Invoke(responseContent);
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
            return _httpClient.GetStreamAsync(url);
        }

        private void Log(string context, string message)
        {
            const int maxMessageLen = 32000;
            
            if (message.Length >= maxMessageLen)
                message = message.Substring(0, maxMessageLen);
                
            _lykkeLog.WriteInfoAsync(
                nameof(ApiClient),
                nameof(ApiClient),
                context,
                message);
        }
    }
}
