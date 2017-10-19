﻿using System;
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

        private const string _balanceRequestUrl = @"/v1/balances";
        private const string _newOrderRequestUrl = @"/v1/order/new";
        private const string _orderStatusRequestUrl = @"/v1/order/status";
        private const string _orderCancelRequestUrl = @"/v1/order/cancel";
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

        public async Task<GdaxOrder> AddOrder(string symbol, decimal amount, decimal price, string side, string type, 
            CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<GdaxOrder>(HttpMethod.Post, _newOrderRequestUrl,
                new GdaxNewOrderPost
                {
                    Symbol = symbol,
                    Amount = amount,
                    Price = price,
                    Exchange = _exchangeName,
                    Side = side,
                    Type = type,
                }, cancellationToken);

            return response;
        }

        public async Task<GdaxOrder> CancelOrder(Guid orderId, CancellationToken cancellationToken = default )
        {
            var response = await ExecuteRestMethod<GdaxOrder>(HttpMethod.Post, _orderCancelRequestUrl,
                new GdaxOrderStatusPost
                {
                    OrderId = orderId
                }, cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<GdaxOrder>> GetOpenOrders(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<IReadOnlyList<GdaxOrder>>(HttpMethod.Get, _activeOrdersRequestUrl,
                new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<GdaxOrder> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default)
        {
            var response = await ExecuteRestMethod<GdaxOrder>(HttpMethod.Get, _orderStatusRequestUrl,
                new GdaxOrderStatusPost
                {
                    OrderId = orderId
                }, cancellationToken);

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
                    return SafeJsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), DeserializationSettings);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    throw new StatusCodeException(response.StatusCode, 
                        JsonConvert.DeserializeObject<Error>(await response.Content.ReadAsStringAsync(), DeserializationSettings).Message);
                default:
                    throw new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", response.StatusCode));
            }
        }

        #endregion
    }
}
