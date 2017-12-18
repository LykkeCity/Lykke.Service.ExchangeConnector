using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Lykke.ExternalExchangesApi.Exceptions;
using Lykke.ExternalExchangesApi.Exchanges.Abstractions;
using Lykke.ExternalExchangesApi.Helpers;
using TradingBot.Exchanges.Concrete.Kraken.Entities;
using TradingBot.Exchanges.Concrete.Kraken.Requests;
using TradingBot.Exchanges.Concrete.Kraken.Responses;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Infrastructure.Exceptions;
using TradingBot.Infrastructure.Logging;
using TradingBot.Trading;
using TradingBot.Repositories;

namespace TradingBot.Exchanges.Concrete.Kraken.Endpoints
{
    public class PrivateData
    {
        private readonly ILogger logger = Logging.CreateLogger<PrivateData>();
        
        private readonly string endpointUrl = $"{Urls.ApiBase}/0/private";
        private const string ApiKeyHeader = "API-Key";
        private const string ApiSignHeader = "API-Sign";

        private readonly ApiClient apiClient;
        private readonly string apiKey;
        private readonly string apiPrivateKey;
        private readonly NonceProvider nonceProvider;
        private readonly IReadOnlyCollection<CurrencySymbol> _currencySymbols;

        public PrivateData(ApiClient apiClient, string apiKey, string apiPrivateKey, NonceProvider nonceProvider, 
            IReadOnlyCollection<CurrencySymbol> currencySymbols)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            this.apiKey = apiKey;
            this.apiPrivateKey = apiPrivateKey;
            this.nonceProvider = nonceProvider;
            _currencySymbols = currencySymbols;
        }

        public Task<Dictionary<string, decimal>> GetAccountBalance(TranslatedSignalTableEntity translatedSignal, CancellationToken cancellationToken)
        {
            return MakePostRequestAsync<Dictionary<string, decimal>>("Balance", new AccountBalanceRequest(), translatedSignal, cancellationToken);
        }

        public Task<TradeBalanceInfo> GetTradeBalance(TranslatedSignalTableEntity translatedSignal, CancellationToken cancellationToken)
        {
            var request = new TradeBalanceRequest()
            {
                AssetClass = "currency",
                BaseAsset = "ZUSD"
            };
            
            return MakePostRequestAsync<TradeBalanceInfo>("TradeBalance", request, translatedSignal, cancellationToken);
        }

        public Task<Dictionary<string, OrderInfo>> GetOpenOrders(CancellationToken cancellationToken)
        {
            return MakePostRequestAsync<Dictionary<string, OrderInfo>>("OpenOrders", new OpenOrdersRequest(), null, cancellationToken);
        }

        public Task<ClosedOrdersResponse> GetClosedOrders(DateTime start, CancellationToken cancellationToken)
        {
            return MakePostRequestAsync<ClosedOrdersResponse>("ClosedOrders", new ClosedOrdersRequest() { Start = DateTimeUtils.ToUnix(start)}, null, cancellationToken);
        }

        public Task<AddStandardOrderResponse> AddOrder(TradingSignal tradingSignal, TranslatedSignalTableEntity translatedSignal, CancellationToken cancellationToken)
        {
            var request = new AddStandardOrderRequest(tradingSignal, _currencySymbols);

            return MakePostRequestAsync<AddStandardOrderResponse>("AddOrder", request, translatedSignal, cancellationToken);
        }

        public Task<CancelOrderResult> CancelOrder(string txId, TranslatedSignalTableEntity translatedSignal)
        {
            var request = new CancelOrderRequest(txId);

            return MakePostRequestAsync<CancelOrderResult>("CancelOrder", request, translatedSignal, CancellationToken.None);
        }


        private DateTime lastRequestTime = DateTime.UtcNow;
        
        private async Task<T> MakePostRequestAsync<T>(string url, IKrakenRequest request, TranslatedSignalTableEntity translatedSignal, CancellationToken cancellationToken)
        {
            try
            {
                var now = DateTime.UtcNow;

                if ((now - lastRequestTime).TotalSeconds <= 2)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
                lastRequestTime = DateTime.UtcNow;

                var content = CreateHttpContent(request, nonceProvider.GetNonce(), url);

                var response = await apiClient.MakePostRequestAsync<ResponseBase<T>>($"{endpointUrl}/{url}", content,
                    translatedSignal.RequestSent, translatedSignal.ResponseReceived, cancellationToken);

                if (response.Error.Any())
                {
                    if (response.Error.Any(x => x == "EOrder:Insufficient funds"))
                    {
                        throw new InsufficientFundsException();
                    }
                    else
                    {
                        throw new ApiException(string.Join("; ", response.Error));    
                    }
                }

                return response.Result;
            }
            catch (Exception e)
            {
                translatedSignal?.Failure(e);
                throw;
            }
        }

        private FormUrlEncodedContent CreateHttpContent(IKrakenRequest requestData, long nonce, string uriPath)
        {
            var pathBytes = Encoding.UTF8.GetBytes($"/0/private/{uriPath}");
            var props = "nonce=" + nonce + string.Join("", requestData.FormData.Select(x => $"&{x.Key}={x.Value}"));
            var np = nonce + Convert.ToChar(0) + props;
            var hash = Sha256(np);
            
            logger.LogDebug($"Making a request with props {props}");

            var z = new byte[pathBytes.Length + hash.Length];
            pathBytes.CopyTo(z, 0);
            hash.CopyTo(z, pathBytes.Length);

            var signature = HmacSha512(Convert.FromBase64String(apiPrivateKey), z);
            var sigString = Convert.ToBase64String(signature);
            
            var data = new List<KeyValuePair<string, string>>();
            data.Add(new KeyValuePair<string, string>("nonce", nonce.ToString()));
            data.AddRange(requestData.FormData);
            
            var content = new FormUrlEncodedContent(data);
            content.Headers.Add(ApiKeyHeader, new [] { apiKey });
            content.Headers.Add(ApiSignHeader, new [] { sigString });

            return content;
        }

        private byte[] Sha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            }
        }

        private byte[] HmacSha512(byte[] key, byte[] message)
        {
            using (var hmac = new HMACSHA512(key))
            {
                return hmac.ComputeHash(message);
            }
        }
    }
}
