using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Exchanges.Abstractions.RestClient;
using TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient
{
    internal sealed class GeminiRestApi : ServiceClient<GeminiRestApi>, IGeminiRestApi
    {
        public const string GeminiPublicApiUrl = @"https://api.gemini.com";
        public const string GeminiSandboxApiUrl = @"https://api.sandbox.gemini.com";

        private const string _balanceRequestUrl = @"/accounts";
        private const string _newOrderRequestUrl = @"/orders";
        private const string _orderCancelRequestUrl = @"/orders/{0}";
        private const string _activeOrdersRequestUrl = @"/orders";
        
        private const string _defaultConnectorUserAgent = "Lykke";
        
        private readonly ServiceClientCredentials _credentials;
        private readonly RestApiClient _restClient;

        /// <summary>
        /// Base Gemini Uri
        /// </summary>
        public Uri BaseUri
        {
            get { return HttpClient.BaseAddress; }
            set { HttpClient.BaseAddress = value; }
        }
        
        /// <summary>
        /// User agent for identification
        /// </summary>
        public string ConnectorUserAgent
        {
            get { return HttpClient.DefaultRequestHeaders.UserAgent.ToString(); }
            set
            {
                HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
            }
        }

        public GeminiRestApi(string apiKey, string apiSecret, string passPhrase)
        {
            _credentials = new GeminiRestClientCredentials(apiKey, apiSecret, passPhrase);

            BaseUri = new Uri(GeminiPublicApiUrl);
            ConnectorUserAgent = _defaultConnectorUserAgent;

            _restClient = new RestApiClient(HttpClient, _credentials);
        }

        public async Task<GeminiOrderResponse> AddOrder(string productId, decimal amount, decimal price,
            GeminiOrderSide side, GeminiOrderType type, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GeminiOrderResponse>(
                HttpMethod.Post, _newOrderRequestUrl,
                new GeminiNewOrderPost
                {
                    ProductId = productId,
                    Size = amount,
                    Price = price,
                    Side = side,
                    Type = type,
                }, cancellationToken, sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<IReadOnlyCollection<Guid>> CancelOrder(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyCollection<Guid>>(HttpMethod.Delete, 
                string.Format(_orderCancelRequestUrl, orderId), new GeminiPostContentBase(), cancellationToken, 
                sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<IReadOnlyList<GeminiOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GeminiOrderResponse>>(HttpMethod.Get, 
                _activeOrdersRequestUrl, new GeminiPostContentBase(), cancellationToken, sentHttpRequestHandler, 
                receivedHttpRequestHandler);

            return response;
        }

        public async Task<GeminiOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GeminiOrderResponse>(HttpMethod.Get,
                string.Format(_orderCancelRequestUrl, orderId), new GeminiPostContentBase(), cancellationToken, 
                sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<IReadOnlyList<GeminiBalanceResponse>> GetBalances(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GeminiBalanceResponse>>(HttpMethod.Get, 
                _balanceRequestUrl, new GeminiPostContentBase(), cancellationToken, sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }
    }
}
