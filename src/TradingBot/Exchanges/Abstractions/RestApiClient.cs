using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Abstractions.RestClient
{
    internal class RestApiClient
    {
        private readonly ServiceClientCredentials _credentials;

        /// <summary>
        /// Gets the used HttpClient
        /// </summary>
        public HttpClient HttpClient { get; private set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; set; }

        public RestApiClient(HttpClient httpClient, ServiceClientCredentials credentials)
        {
            HttpClient = httpClient;
            _credentials = credentials;

            SerializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter(),
                    new DecimalToStringJsonConverter(),
                    new StringEnumConverter()
                }
            };
            DeserializationSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter()
                }
            };
        }

        public async Task<T> ExecuteRestMethod<T>(HttpMethod httpMethod, string relativePath, PostContentBase bodyContent,
            CancellationToken cancellationToken, EventHandler<SentHttpRequest> sentHttpRequestHandler, 
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler)
        {
            using (var request = await GetRestRequest(httpMethod, relativePath, bodyContent, cancellationToken))
            {
                sentHttpRequestHandler?.Invoke(this, 
                    new SentHttpRequest(HttpMethod.Post, request.RequestUri, request.Content));

                using (var response = await HttpClient.SendAsync(request, cancellationToken))
                {
                    var content = await response.Content.ReadAsStringAsync()
                        .WithCancellation(cancellationToken).ConfigureAwait(false);

                    receivedHttpRequestHandler?.Invoke(this, new ReceivedHttpResponse(content));

                    var responseBody = DeserializeResponse<T>(response.StatusCode, content);
                    return responseBody;
                }
            }
        }

        private async Task<HttpRequestMessage> GetRestRequest(HttpMethod httpMethod, string relativeUrl,
            PostContentBase bodyContent, CancellationToken cancellationToken)
        {
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(HttpClient.BaseAddress, relativeUrl)
            };

            var jsonObj = SafeJsonConvert.SerializeObject(bodyContent, SerializationSettings);
            httpRequest.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");

            if (_credentials != null)
                await _credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            return httpRequest;
        }

        private T DeserializeResponse<T>(HttpStatusCode statusCode, string content)
        {
            if (!IsSuccessHttpStatusCode(statusCode))
                throw new StatusCodeException(statusCode, content);

            return SafeJsonConvert.DeserializeObject<T>(content, DeserializationSettings);
        }

        private bool IsSuccessHttpStatusCode(HttpStatusCode statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 299); 
        }
    }
}
