using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions.Models;
using Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities;
using Microsoft.Rest;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient
{
    public sealed class GdaxRestApi : ServiceClient<GdaxRestApi>, IGdaxRestApi
    {
        public const string GdaxPublicApiUrl = @"https://api.gdax.com";
        public const string GdaxSandboxApiUrl = @"https://api-public.sandbox.gdax.com";

        private const string _balanceRequestUrl = @"/accounts";
        private const string _newOrderRequestUrl = @"/orders";
        private const string _orderCancelRequestUrl = @"/orders/{0}";
        private const string _activeOrdersRequestUrl = @"/orders";
        private const string _orderBookRequestUrl = @"/products/{0}/book?level=3";
        
        private const string _defaultConnectorUserAgent = "Lykke";

        private readonly RestApiClient _restClient;
        
        public GdaxRestApi(string apiKey, string apiSecret, string passPhrase) :
            this (apiKey, apiSecret, passPhrase, GdaxPublicApiUrl, _defaultConnectorUserAgent)
        { }

        public GdaxRestApi(string apiKey, string apiSecret, string passPhrase, 
            string publicApiUrl, string userAgent)
        {
            var credentials = !string.IsNullOrEmpty(apiKey) 
                ? new GdaxRestClientCredentials(apiKey, apiSecret, passPhrase)
                : null;

            HttpClient.BaseAddress = new Uri(publicApiUrl);
            
            HttpClient.DefaultRequestHeaders.UserAgent.Clear();
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            _restClient = new RestApiClient(HttpClient, credentials);
        }

        public async Task<GdaxOrderResponse> AddOrder(string symbol, decimal amount, decimal price,
            GdaxOrderSide side, GdaxOrderType type, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderResponse>(
                HttpMethod.Post, _newOrderRequestUrl,
                new GdaxNewOrderPost
                {
                    ProductId = symbol,
                    Size = amount,
                    Price = price,
                    Side = side,
                    Type = type,
                }, cancellationToken, sentHttpRequestHandler, receivedHttpRequestHandler);

            return response;
        }

        public async Task<bool> CancelOrder(Guid orderId, CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GdaxError>>(HttpMethod.Delete, 
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostContentBase(), cancellationToken, 
                sentHttpRequestHandler, receivedHttpRequestHandler);
            if (response[0] == null)
                return true;

            return false;
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

        public async Task<GdaxOrderBook> GetFullOrderBook(string pair, 
            CancellationToken cancellationToken = default,
            EventHandler<SentHttpRequest> sentHttpRequestHandler = default,
            EventHandler<ReceivedHttpResponse> receivedHttpRequestHandler = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderBookRawResponse>(HttpMethod.Get,
                string.Format(_orderBookRequestUrl, pair), new GdaxPostContentBase(), cancellationToken, sentHttpRequestHandler,
                receivedHttpRequestHandler);

            var orderBook = new GdaxOrderBook
            {
                Sequence = response.Sequence,
                Asks = response.Asks.Select(ask => new GdaxOrderBookEntityRow
                {
                    Price = decimal.Parse(ask[0], CultureInfo.InvariantCulture),
                    Size = decimal.Parse(ask[1], CultureInfo.InvariantCulture),
                    OrderId = Guid.Parse(ask[2])
                }).ToList(),
                Bids = response.Bids.Select(bid => new GdaxOrderBookEntityRow
                {
                    Price = decimal.Parse(bid[0], CultureInfo.InvariantCulture),
                    Size = decimal.Parse(bid[1], CultureInfo.InvariantCulture),
                    OrderId = Guid.Parse(bid[2])
                }).ToList()
            };

            return orderBook;
        }
    }
}
