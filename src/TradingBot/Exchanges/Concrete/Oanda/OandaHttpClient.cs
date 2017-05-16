using System.Net.Http;

namespace TradingBot.Exchanges.Concrete.Oanda
{
    public class OandaHttpClient
    {
        public static HttpClient CreateHttpClient(string token)
        {
            var client = new HttpClient();

            string auth = $"Bearer {token}";

            client.DefaultRequestHeaders.Add("Authorization", auth);
            client.DefaultRequestHeaders.Add("AcceptEncoding", new[] { "gzip", "deflate" });
            client.DefaultRequestHeaders.Add("ContentType", "application/json");

            return client;
        }
    }
}
