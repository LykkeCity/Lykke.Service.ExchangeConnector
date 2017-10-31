using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using TradingBot.Helpers;

namespace TradingBot.Exchanges.Concrete.Gemini.Credentials
{
    internal sealed class GeminiCredentialsFactory
    {
        public string ApiKey { get; private set; }
        public string ApiSecret { get; private set; }
        public string PassPhrase { get; private set; }

        public GeminiCredentialsFactory(string apiKey, string apiSecret, string passPhrase)
        {
            ApiKey = apiKey;
            ApiSecret = apiSecret;
            PassPhrase = passPhrase;
        }

        public GeminiCredentials GenerateCredentials(HttpMethod method, Uri requestUri, string content)
        {
            var unixTimestamp = DateTime.UtcNow.ToUnixTimestampInt();
            var unixTimestampString = unixTimestamp
                .ToString(System.Globalization.CultureInfo.InvariantCulture);

            var signature = GetGeminiHashedSignature(method, requestUri, content, unixTimestampString);

            var credentials = new GeminiCredentials(apiKey: ApiKey, apiSecret: ApiSecret, passPhrase: PassPhrase, 
                unixTimestampString: unixTimestampString, signature: signature);
            return credentials;
        }

        private string GetGeminiHashedSignature(HttpMethod method, Uri requestUri, string content, string timeStampString)
        {
            var signature = timeStampString + method.ToString().ToUpper() +
                    requestUri.AbsolutePath + content;
            var apiSecretFromBase64 = Convert.FromBase64String(ApiSecret);

            return HashString(signature, apiSecretFromBase64);
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
