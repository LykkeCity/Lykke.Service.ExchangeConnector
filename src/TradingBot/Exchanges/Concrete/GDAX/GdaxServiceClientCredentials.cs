using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using TradingBot.Helpers;

namespace TradingBot.Exchanges.Concrete.GDAX
{
    internal sealed class GdaxServiceClientCredentials : ServiceClientCredentials
    {
        private const string _accessKeyHeader = "CB-ACCESS-KEY";
        private const string _accessSignatureHeader = "CB-ACCESS-SIGN";
        private const string _timeStampHeader = "CB-ACCESS-TIMESTAMP";
        private const string _passPhraseHeader = "CB-ACCESS-PASSPHRASE";

        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _passPhrase;

        public GdaxServiceClientCredentials(string apiKey, string apiSecret, string passPhrase)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _passPhrase = passPhrase;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var unixTime = DateTime.UtcNow.ToUnixTimestampInt()
                .ToString(System.Globalization.CultureInfo.InvariantCulture);

            var signature = await GetGdaxHashedSignature(request, unixTime, cancellationToken);

            request.Headers.Add(_accessKeyHeader, _apiKey);
            request.Headers.Add(_accessSignatureHeader, signature);
            request.Headers.Add(_timeStampHeader, unixTime);
            request.Headers.Add(_passPhraseHeader, _passPhrase);
        }

        private async Task<string> GetGdaxHashedSignature(HttpRequestMessage request, string timeStampString, 
            CancellationToken cancellationToken)
        {
            var content = await GetContent(request, cancellationToken);

            var signature = timeStampString + request.Method.ToString().ToUpper() + 
                    request.RequestUri.AbsolutePath + content;
            var apiSecretFromBase64 = Convert.FromBase64String(_apiSecret);

            return HashString(signature, apiSecretFromBase64);
        }

        private static async Task<string> GetContent(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // ReadAsStringAsync() doesn't have an overaload accepting cancellationToken, that's why we are forcing the 
            // operation to be cancelled, even though the method will continue working in background until it finishes execution
            return request.Content == null 
                ? string.Empty 
                : await request.Content.ReadAsStringAsync()   
                    .WithCancellation(cancellationToken);    
        }

        private string HashString(string str, byte[] secret)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var hmac = new HMACSHA256(secret))
            {
                var hash = hmac.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
