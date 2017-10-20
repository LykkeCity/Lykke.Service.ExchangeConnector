using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal class ApiRestClient
    {
        private readonly ServiceClientCredentials _credentials;
        private readonly ILog _log;
        private static readonly string _apiRestClientName = nameof(ApiRestClient);

        /// <summary>
        /// Gets the used HttpClient
        /// </summary>
        public HttpClient HttpClient { get; private set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        public ApiRestClient(HttpClient httpClient, ServiceClientCredentials credentials, ILog log)
        {
            HttpClient = httpClient;
            _credentials = credentials;
            _log = log;

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

        public async Task<T> ExecuteRestMethod<T>(HttpMethod httpMethod, string relativePath, GdaxPostBase bodyContent,
            CancellationToken cancellationToken)
        {
            using (var request = await GetRestRequest(httpMethod, relativePath, bodyContent, cancellationToken))
            {
                Log($"Making request to URL: {request.RequestUri}");
                using (var response = await HttpClient.SendAsync(request, cancellationToken))
                {
                    var responseBody = await DeserializeResponse<T>(response, cancellationToken);
                    return responseBody;
                }
            }
        }

        private async Task<HttpRequestMessage> GetRestRequest(HttpMethod httpMethod, string relativeUrl,
            GdaxPostBase bodyContent, CancellationToken cancellationToken)
        {
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(HttpClient.BaseAddress, relativeUrl)
            };

            var jsonObj = SafeJsonConvert.SerializeObject(bodyContent, SerializationSettings);
            httpRequest.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");

            await _credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            return httpRequest;
        }

        private async Task<T> DeserializeResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync()
                .WithCancellation(cancellationToken).ConfigureAwait(false);
            Log($"Received content: {content}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return SafeJsonConvert.DeserializeObject<T>(content, DeserializationSettings);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    throw new StatusCodeException(response.StatusCode,
                        JsonConvert.DeserializeObject<GdaxError>(content, DeserializationSettings).Message);
                default:
                    throw new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", response.StatusCode));
            }
        }

        private void Log(string message, [CallerMemberName]string context = null)
        {
            const int maxMessageLength = 32000;

            if (_log == null)
                return;
 
            if (message.Length >= maxMessageLength)
                message = message.Substring(0, maxMessageLength);

            _log.WriteInfoAsync(_apiRestClientName, _apiRestClientName, context, message);
        }
    }
}
