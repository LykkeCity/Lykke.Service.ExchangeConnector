using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Exchanges.Abstractions.RestClient;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal sealed class GdaxRestApi : ServiceClient<GdaxRestApi>, IGdaxRestApi
    {
        public const string GdaxPublicApiUrl = @"https://api.gdax.com";
        public const string GdaxSandboxApiUrl = @"https://api-public.sandbox.gdax.com";

        private const string _userAgentHeaderName = "User-Agent";
        private const string _balanceRequestUrl = @"/accounts";
        private const string _newOrderRequestUrl = @"/orders";
        private const string _orderStatusRequestUrl = @"/orders/{0}&status=done&status=pending&status=open&status=cancelled";
        private const string _orderCancelRequestUrl = @"/orders/{0}";
        private const string _activeOrdersRequestUrl = @"/orders";
        private const string _marginInfoRequstUrl = @"/v1/margin_infos";
        
        private const string _defaultConnectorUserAgent = "Lykke";
        
        private readonly ServiceClientCredentials _credentials;
        private RestApiClient _restClient;

        public GdaxRestApi(string apiKey, string apiSecret, string passPhrase, string publicApiUrl = null, string userAgent = null)
        {
            _credentials = new GdaxRestClientCredentials(apiKey, apiSecret, passPhrase);

            HttpClient.BaseAddress = new Uri(string.IsNullOrEmpty(publicApiUrl) ? GdaxPublicApiUrl : publicApiUrl);
            
            HttpClient.DefaultRequestHeaders.UserAgent.Clear();
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                string.IsNullOrEmpty(userAgent) ? _defaultConnectorUserAgent : userAgent);

            _restClient = new RestApiClient(HttpClient, _credentials);
        }

        public async Task<GdaxOrderResponse> AddOrder(string productId, decimal amount, decimal price,
            GdaxOrderSide side, GdaxOrderType type, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderResponse>(
                HttpMethod.Post, _newOrderRequestUrl,
                new GdaxNewOrderPost
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
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostContentBase(), cancellationToken, 
                sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<IReadOnlyList<GdaxOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GdaxOrderResponse>>(HttpMethod.Get, 
                _activeOrdersRequestUrl, new GdaxPostContentBase(), cancellationToken, sentHttpRequestHandler, 
                receivedHttpRequestHandler);

            return response;
        }

        public async Task<GdaxOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderResponse>(HttpMethod.Get,
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostContentBase(), cancellationToken, 
                sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GdaxBalanceResponse>>(HttpMethod.Get, 
                _balanceRequestUrl, new GdaxPostContentBase(), cancellationToken, sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }
    }
}
