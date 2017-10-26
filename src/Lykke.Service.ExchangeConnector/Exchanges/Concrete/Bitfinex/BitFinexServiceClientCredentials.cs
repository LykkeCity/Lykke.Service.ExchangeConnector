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
            HMACSHA384 hmac = new HMACSHA384(Encoding.UTF8.GetBytes(_apiSecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static async Task<string> GetContent(HttpRequestMessage request)
        {
            return request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();
        }


        private static long GetNonce()
        {
            var yearBegin = new DateTime(1990, 1, 1);
            return DateTime.UtcNow.Ticks - yearBegin.Ticks;
        }

        private byte[] hmacsha256(byte[] keyByte, byte[] messageBytes)
        {
            using (var hash = new HMACSHA256(keyByte))
            {
                return hash.ComputeHash(messageBytes);
            }
        }

        private static string ByteArrayToString(IReadOnlyCollection<byte> ba)
        {
            var hex = new StringBuilder(ba.Count * 2);
            foreach (var b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}
