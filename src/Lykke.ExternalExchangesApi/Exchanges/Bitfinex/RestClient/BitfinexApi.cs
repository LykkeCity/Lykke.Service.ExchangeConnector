using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model;
using Lykke.ExternalExchangesApi.Shared;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Helpers;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient
{
    public sealed class BitfinexApi : ServiceClient<BitfinexApi>, IBitfinexApi
    {
        private const string BalanceRequestUrl = @"/v1/balances";
        private const string NewOrderRequestUrl = @"/v1/order/new";
        private const string OrderStatusRequestUrl = @"/v1/order/status";
        private const string OrderCancelRequestUrl = @"/v1/order/cancel";

        private const string ActiveOrdersRequestUrl = @"/v1/orders";
        private const string ActivePositionsRequestUrl = @"/v1/positions";
        private const string MarginInfoRequstUrl = @"/v1/margin_infos";
        private const string AllSymbolsRequestUrl = @"/v1/symbols";


        private const string BaseBitfinexUrl = @"https://api.bitfinex.com";

        private const string Exchange = "bitfinex";


        public Uri BaseUri { get; set; }

        private readonly BitfinexServiceClientCredentials _credentials;

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        public BitfinexApi(BitfinexServiceClientCredentials credentials)
        {
            _credentials = credentials;
            Initialize();
        }

        private void Initialize()
        {
            BaseUri = new Uri(BaseBitfinexUrl);
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


        public async Task<object> AddOrder(string symbol, decimal amount, decimal price, string side, string type, CancellationToken cancellationToken = default)
        {
            var newOrder = new BitfinexNewOrderPost
            {
                Symbol = symbol,
                Amount = amount,
                Price = price,
                Exchange = Exchange,
                Side = side,
                Type = type,
                Request = NewOrderRequestUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString()
            };

            var response = await GetRestResponse<Order>(newOrder, cancellationToken);

            return response;
        }

        public async Task<object> CancelOrder(long orderId, CancellationToken cancellationToken = default)
        {
            var cancelPost = new BitfinexOrderStatusPost
            {
                Request = OrderCancelRequestUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString(),
                OrderId = orderId
            };

            var response = await GetRestResponse<Order>(cancelPost, cancellationToken);

            return response;
        }

        public async Task<object> GetActiveOrders(CancellationToken cancellationToken = default)
        {
            var activeOrdersPost = new BitfinexPostBase
            {
                Request = ActiveOrdersRequestUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString()
            };

            var response = await GetRestResponse<IReadOnlyList<Order>>(activeOrdersPost, cancellationToken);

            return response;
        }


        public async Task<object> GetOrderStatus(long orderId, CancellationToken cancellationToken = default)
        {
            var orderStatusPost = new BitfinexOrderStatusPost
            {
                Request = OrderStatusRequestUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString(),
                OrderId = orderId
            };

            var response = await GetRestResponse<Order>(orderStatusPost, cancellationToken);

            return response;
        }


        public async Task<object> GetBalances(CancellationToken cancellationToken = default)
        {
            var balancePost = new BitfinexPostBase();
            balancePost.Request = BalanceRequestUrl;
            balancePost.Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString();

            var response = await GetRestResponse<IReadOnlyList<BitfinexBalanceResponse>>(balancePost, cancellationToken);

            return response;
        }


        public async Task<object> GetMarginInformation(CancellationToken cancellationToken = default)
        {
            var marginPost = new BitfinexPostBase
            {
                Request = MarginInfoRequstUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString()
            };


            var response = await GetRestResponse<IReadOnlyList<MarginInfo>>(marginPost, cancellationToken);

            return response;
        }

        public async Task<object> GetActivePositions(CancellationToken cancellationToken = default)
        {
            var activePositionsPost = new BitfinexPostBase
            {
                Request = ActivePositionsRequestUrl,
                Nonce = UnixTimeConverter.UnixTimeStampUtc().ToString()
            };

            var response = await GetRestResponse<IReadOnlyList<Position>>(activePositionsPost, cancellationToken);

            return response;
        }

        public async Task<object> GetAllSymbols(CancellationToken cancellationToken = default)
        {
            var response = await GetRestResponse<IReadOnlyList<string>>(new BitfinexGetBase{Request = AllSymbolsRequestUrl }, cancellationToken);

            return response;
        }

        private async Task<object> GetRestResponse<T>(BitfinexPostBase obj, CancellationToken cancellationToken)
        {
            using (var request = await GetRestRequest(obj, cancellationToken))
            {
                return await SendHttpRequestAndGetResponse<T>(request, cancellationToken);
            }
        }

        private async Task<object> GetRestResponse<T>(BitfinexGetBase obj, CancellationToken cancellationToken)
        {
            using (var request = GetRestRequest(obj))
            {
                return await SendHttpRequestAndGetResponse<T>(request, cancellationToken);
            }
            
        }

        private Task<HttpRequestMessage> GetRestRequest(BitfinexPostBase obj, CancellationToken cancellationToken)
        {
            return EpochNonce.Lock(_credentials.ApiKey, async nonce =>
            {

                obj.Nonce = nonce.ToString(CultureInfo.InvariantCulture);

                // Create HTTP transport objects
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod("POST"),
                    RequestUri = new Uri(BaseUri, obj.Request)
                };

                var jsonObj =
                    SafeJsonConvert.SerializeObject((object) obj, (JsonSerializerSettings) SerializationSettings);
                httpRequest.Content = new StringContent(jsonObj, Encoding.UTF8, "application/json");

                await _credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

                return httpRequest;
            }, LockKind.EpochMilliseconds);
        }

        private HttpRequestMessage GetRestRequest(BitfinexGetBase obj)
        {

            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri(BaseUri, obj.Request)
            };

            return httpRequest;
        }


        private async Task<object> SendHttpRequestAndGetResponse<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var response = await HttpClient.SendAsync(request, cancellationToken))
            {
                var responseBody = await CheckError<T>(response);
                return responseBody;
            }
        }

        private async Task<object> CheckError<T>(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return SafeJsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), DeserializationSettings);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    return JsonConvert.DeserializeObject<Error>((string) await response.Content.ReadAsStringAsync(), (JsonSerializerSettings) DeserializationSettings);
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
