using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TradingBot.OandaApi.Entities.Accounts;
using TradingBot.Exceptions;
using System.IO;

namespace TradingBot.OandaApi
{
    public class ApiClient
    {
        public ApiClient(string token)
        {
            httpClient = CreateHttpClient(token);
        }

        private readonly HttpClient httpClient;

        private HttpClient CreateHttpClient(string token)
        {
            var client = new HttpClient();
            
            string auth = $"Bearer {token}";

            client.DefaultRequestHeaders.Add("Authorization", auth);
            client.DefaultRequestHeaders.Add("AcceptEncoding", new[] { "gzip", "deflate"});
            client.DefaultRequestHeaders.Add("ContentType", "application/json");

            return client;
        }

        public Task<AccountInstruments> GetAccountInstruments(string accountId)
        {
            return MakeRequestAsync<AccountInstruments>($"{OandaUrls.Accounts}/{accountId}/instruments");
        }

        public async Task<TResponse> MakeRequestAsync<TResponse>(string url, string method = "GET")
        {
            Log($"Making request to url: {url}");

            using (var response = await httpClient.GetAsync(url))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();

                    throw new ApiException($"Unexpected status code: {response.StatusCode}. {errorContent}");
                }

                string content = await response.Content.ReadAsStringAsync();

                Log($"Received content: {content}");

                var result = JsonConvert.DeserializeObject<TResponse>(content);

                return result;
            }
        }

        public async Task<Stream> MakeStreamRequestAsync(string url)
        {
            return await httpClient.GetStreamAsync(url);
        }

        private void Log(string log)
        {
            Console.WriteLine(log);
        }
    }
}