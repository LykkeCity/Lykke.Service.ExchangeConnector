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
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;
using TradingBot.Helpers;
using TradingBot.Infrastructure.Exceptions;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal sealed class GdaxApi : ServiceClient<GdaxApi>, IGdaxApi
    {
        public const string GdaxPublicApiUrl = @"https://api.gdax.com";
        public const string GdaxSandboxApiUrl = @"https://api-public.sandbox.gdax.com";

        private const string _balanceRequestUrl = @"/accounts";
        private const string _newOrderRequestUrl = @"/orders";
        private const string _orderStatusRequestUrl = @"/orders/{0}";
        private const string _orderCancelRequestUrl = @"/orders/{0}";
        private const string _activeOrdersRequestUrl = @"/orders";
        private const string _marginInfoRequstUrl = @"/v1/margin_infos";
        
        private const string _exchangeName = "GDAX";
        private const string _defaultConnectorUserAgent = "Lykke";
        
        private readonly ServiceClientCredentials _credentials;

        /// <summary>
        /// Base GDAX Uri
        /// </summary>
        public Uri BaseUri { get; set; }
        
        /// <summary>
        /// User agent for identification
        /// </summary>
        public string ConnectorUserAgent { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        public GdaxApi(ServiceClientCredentials credentials)
        {
            _credentials = credentials;
            Initialize();
        }

        private void Initialize()
        {
            BaseUri = new Uri(GdaxPublicApiUrl);
            ConnectorUserAgent = _defaultConnectorUserAgent;
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
                    new DecimalToStringJsonConverter()
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

        public async Task<GdaxOrderResponse> AddOrder(string productId, decimal amount, decimal price,
            GdaxOrderSide side, GdaxOrderType type, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<GdaxOrderResponse>(HttpMethod.Post, _newOrderRequestUrl,
                new GdaxNewOrderPost
                {
                    ProductId = productId,
                    Size = amount,
                    Price = price,
                    Side = side,
                    Type = type,
                }, cancellationToken);

            return response;
        }

        public async Task<IReadOnlyCollection<Guid>> CancelOrder(Guid orderId, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<IReadOnlyCollection<Guid>>(HttpMethod.Delete, 
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<GdaxOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<IReadOnlyList<GdaxOrderResponse>>(HttpMethod.Get, _activeOrdersRequestUrl,
                new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<GdaxOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<GdaxOrderResponse>(HttpMethod.Get,
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<IReadOnlyList<GdaxBalanceResponse>>(HttpMethod.Get, _balanceRequestUrl,
                new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyCollection<GdaxMarginInfoResponse>> GetMarginInformation(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<IReadOnlyCollection<GdaxMarginInfoResponse>>(HttpMethod.Get, 
                _marginInfoRequstUrl, new GdaxPostBase(), cancellationToken);

            return response;
        }

        #region Private Methods

        private async Task<T> ExecuteRestMethod<T>(HttpMethod httpMethod, string relativePath, GdaxPostBase bodyContent, 
            CancellationToken cancellationToken)
        {
            using (var request = await ExecuteRestRequest(httpMethod, relativePath, bodyContent, cancellationToken))
            {
                using (var response = await HttpClient.SendAsync(request, cancellationToken))
                {
                    var responseBody = await DeserializeResponse<T>(response);
                    return responseBody;
                }
            }
        }

        private async Task<HttpRequestMessage> ExecuteRestRequest(HttpMethod httpMethod, string relativeUrl,  
            GdaxPostBase bodyContent, CancellationToken cancellationToken)
        {
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(BaseUri, relativeUrl)
            };
            httpRequest.Headers.Add("User-Agent", _defaultConnectorUserAgent);

            var jsonObj = SafeJsonConvert.SerializeObject(bodyContent, SerializationSettings);
            httpRequest.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");

            await _credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            return httpRequest;
        }

        private async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var content = await response.Content.ReadAsStringAsync();
                    return SafeJsonConvert.DeserializeObject<T>(content, DeserializationSettings);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    throw new StatusCodeException(response.StatusCode, 
                        JsonConvert.DeserializeObject<GdaxError>(await response.Content.ReadAsStringAsync(), DeserializationSettings).Message);
                default:
                    throw new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", response.StatusCode));
            }
        }

        #endregion
    }
}
