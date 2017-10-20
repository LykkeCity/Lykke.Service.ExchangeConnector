using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.Rest;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal sealed class GdaxApi : ServiceClient<GdaxApi>, IGdaxApi
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
        private readonly ILog _log;
        private ApiRestClient _restClient;

        /// <summary>
        /// Base GDAX Uri
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

        public GdaxApi(ServiceClientCredentials credentials, ILog log)
        {
            _credentials = credentials;
            _log = log;

            BaseUri = new Uri(GdaxPublicApiUrl);
            ConnectorUserAgent = _defaultConnectorUserAgent;

            _restClient = new ApiRestClient(HttpClient, _credentials, _log);
        }

        public async Task<GdaxOrderResponse> AddOrder(string productId, decimal amount, decimal price,
            GdaxOrderSide side, GdaxOrderType type, CancellationToken cancellationToken = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderResponse>(HttpMethod.Post, _newOrderRequestUrl,
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
            var response = await _restClient.ExecuteRestMethod<IReadOnlyCollection<Guid>>(HttpMethod.Delete, 
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<GdaxOrderResponse>> GetOpenOrders(CancellationToken cancellationToken = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GdaxOrderResponse>>(HttpMethod.Get, _activeOrdersRequestUrl,
                new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<GdaxOrderResponse> GetOrderStatus(Guid orderId, CancellationToken cancellationToken = default)
        {
            var response = await _restClient.ExecuteRestMethod<GdaxOrderResponse>(HttpMethod.Get,
                string.Format(_orderCancelRequestUrl, orderId), new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<GdaxBalanceResponse>> GetBalances(CancellationToken cancellationToken = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyList<GdaxBalanceResponse>>(HttpMethod.Get, _balanceRequestUrl,
                new GdaxPostBase(), cancellationToken);

            return response;
        }

        public async Task<IReadOnlyCollection<GdaxMarginInfoResponse>> GetMarginInformation(CancellationToken cancellationToken = default)
        {
            var response = await _restClient.ExecuteRestMethod<IReadOnlyCollection<GdaxMarginInfoResponse>>(HttpMethod.Get, 
                _marginInfoRequstUrl, new GdaxPostBase(), cancellationToken);

            return response;
        }
    }
}
