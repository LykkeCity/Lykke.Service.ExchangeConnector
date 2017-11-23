using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    public class BitMexServiceClientCredentials : ServiceClientCredentials
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public BitMexServiceClientCredentials(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }


        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var nonce = GetNonce().ToString();
            var content = await GetContent(request);
            var message = request.Method.Method + request.RequestUri.PathAndQuery + nonce + content;
            var signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(_apiSecret), Encoding.UTF8.GetBytes(message));
            var signatureString = ByteArrayToString(signatureBytes);

            request.Headers.Add("api-nonce", nonce);
            request.Headers.Add("api-key", _apiKey);
            request.Headers.Add("api-signature", signatureString);
        }

        public object[] BuildAuthArguments(string path)
        {
            var nonce = GetNonce();
            var message = path + nonce.ToString();
            var signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(_apiSecret), Encoding.UTF8.GetBytes(message));
            var signatureString = ByteArrayToString(signatureBytes);
            return new object[] { _apiKey, nonce, signatureString };
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
