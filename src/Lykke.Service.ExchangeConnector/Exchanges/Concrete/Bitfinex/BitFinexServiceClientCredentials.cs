using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexServiceClientCredentials : ServiceClientCredentials
    {
        private const string ApiBfxKey = "X-BFX-APIKEY";
        private const string ApiBfxPayload = "X-BFX-PAYLOAD";
        private const string ApiBfxSig = "X-BFX-SIGNATURE";

        private readonly string _apiKey;
        private readonly string _apiSecret;

        public BitfinexServiceClientCredentials(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }


        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var jsonObj = await request.Content.ReadAsStringAsync();
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonObj));

            request.Headers.Add(ApiBfxKey, _apiKey);
            request.Headers.Add(ApiBfxPayload, payload);
            request.Headers.Add(ApiBfxSig, GetHexHashSignature(payload));
        }

        private string GetHexHashSignature(string payload)
        {
            var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(_apiSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
