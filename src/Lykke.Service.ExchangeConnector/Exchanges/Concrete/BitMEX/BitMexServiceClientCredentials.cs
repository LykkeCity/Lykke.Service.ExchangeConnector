using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Shared;
using Microsoft.Rest;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal sealed class BitMexServiceClientCredentials : ServiceClientCredentials
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private static readonly TimeSpan ExpirationTimeout = TimeSpan.FromSeconds(5);

        public BitMexServiceClientCredentials(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }


        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var expire = GetExpire();
            var content = await GetContent(request);
            var message = request.Method.Method + request.RequestUri.PathAndQuery + expire + content;
            var signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(_apiSecret), Encoding.UTF8.GetBytes(message));
            var signatureString = ByteArrayToString(signatureBytes);

            request.Headers.Add("api-expires", expire.ToString());
            request.Headers.Add("api-key", _apiKey);
            request.Headers.Add("api-signature", signatureString);
        }

        public object[] BuildAuthArguments(string path)
        {
            var expire = GetExpire();
            var message = path + expire;
            var signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(_apiSecret), Encoding.UTF8.GetBytes(message));
            var signatureString = ByteArrayToString(signatureBytes);
            return new object[] { _apiKey, expire, signatureString };
        }

        private static async Task<string> GetContent(HttpRequestMessage request)
        {
            return request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync();
        }


        private static long GetExpire()
        {
            return UnixTimeConverter.ToUnixTime(DateTime.UtcNow.Add(ExpirationTimeout));
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
