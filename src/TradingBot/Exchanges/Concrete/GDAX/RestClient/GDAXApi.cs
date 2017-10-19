using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.GDAX.RestClient.Model;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient
{
    internal sealed class GdaxApi : ServiceClient<GdaxApi>, IGdaxApi
    {
        private const string BalanceRequestUrl = @"/v1/balances";
        private const string NewOrderRequestUrl = @"/v1/order/new";
        private const string OrderStatusRequestUrl = @"/v1/order/status";
        private const string OrderCancelRequestUrl = @"/v1/order/cancel";

        private const string ActiveOrdersRequestUrl = @"/orders";
        private const string MarginInfoRequstUrl = @"/v1/margin_infos";
        
        private const string BaseGdaxUrl = @"https://api-public.sandbox.gdax.com";  // TODO: Remove sandbox

        private const string Exchange = "GDAX";
        private const string ConnectorUserAgent = "Lykke";
        
        public Uri BaseUri { get; set; }

        private readonly ServiceClientCredentials _credentials;

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
            BaseUri = new Uri(BaseGdaxUrl);
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
                    new StringDecimalConverter()
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

        public async Task<object> AddOrder(string symbol, decimal amount, decimal price, string side, string type, 
            CancellationToken cancellationToken = default)
        {
            var newOrder = new GdaxNewOrderPost
            {
                Symbol = symbol,
                Amount = amount,
                Price = price,
                Exchange = Exchange,
                Side = side,
                Type = type,
                RequestUrl = NewOrderRequestUrl
            };

            var response = await GetRestResponse<GdaxOrder>(newOrder, cancellationToken);

            return response;
        }

        public async Task<object> CancelOrder(long orderId, CancellationToken cancellationToken = default )
        {
            var cancelPost = new GdaxOrderStatusPost
            {
                RequestUrl = OrderCancelRequestUrl,
                OrderId = orderId
            };

            var response = await GetRestResponse<GdaxOrder>(cancelPost, cancellationToken);

            return response;
        }

        public async Task<object> GetOpenOrders(CancellationToken cancellationToken = default)
        {
            var activeOrdersPost = new GdaxPostBase
            {
                RequestUrl = ActiveOrdersRequestUrl
            };

            var response = await GetRestResponse<IReadOnlyList<GdaxOrder>>(activeOrdersPost, cancellationToken);

            return response;
        }


        public async Task<object> GetOrderStatus(long orderId, CancellationToken cancellationToken = default)
        {
            var orderStatusPost = new GdaxOrderStatusPost
            {
                RequestUrl = OrderStatusRequestUrl,
                OrderId = orderId
            };

            var response = await GetRestResponse<GdaxOrder>(orderStatusPost, cancellationToken);

            return response;
        }


        public async Task<object> GetBalances(CancellationToken cancellationToken = default)
        {
            var balancePost = new GdaxPostBase();
            balancePost.RequestUrl = BalanceRequestUrl;

            var response = await GetRestResponse<IReadOnlyList<GdaxBalanceResponse>>(balancePost, cancellationToken);

            return response;
        }


        public async Task<object> GetMarginInformation(CancellationToken cancellationToken = default)
        {
            var marginPost = new GdaxPostBase
            {
                RequestUrl = MarginInfoRequstUrl
            };


            var response = await GetRestResponse<IReadOnlyCollection<GdaxMarginInfoResponse>>(marginPost, cancellationToken);

            return response;
        }

        private async Task<object> GetRestResponse<T>(GdaxPostBase postBase, CancellationToken cancellationToken)
        {
            using (var request = await GetRestRequest(postBase, cancellationToken))
            {
                using (var response = await HttpClient.SendAsync(request, cancellationToken))
                {
                    var responseBody = await CheckError<T>(response);
                    return responseBody;
                }
            }
        }

        private async Task<HttpRequestMessage> GetRestRequest(GdaxPostBase postBase, CancellationToken cancellationToken)
        {
            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri(BaseUri, postBase.RequestUrl)
            };
            httpRequest.Headers.Add("User-Agent", ConnectorUserAgent);

            var jsonObj = SafeJsonConvert.SerializeObject(postBase, SerializationSettings);
            httpRequest.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");

            await _credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            return httpRequest;
        }


        private async Task<object> CheckError<T>(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return SafeJsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), DeserializationSettings);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    return JsonConvert.DeserializeObject<Error>(await response.Content.ReadAsStringAsync(), DeserializationSettings);
                default:
                    throw new HttpOperationException(string.Format("Operation returned an invalid status code '{0}'", response.StatusCode));
            }
        }

        private sealed class StringDecimalConverter : JsonConverter
        {
            public override bool CanRead => false;

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(decimal) || objectType == typeof(decimal?);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((decimal)value).ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
