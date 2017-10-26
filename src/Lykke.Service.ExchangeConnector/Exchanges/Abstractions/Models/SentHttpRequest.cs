using System;
using System.Net.Http;

namespace TradingBot.Exchanges.Abstractions.Models
{
    internal class SentHttpRequest
    {
        public HttpMethod HttpMethod { get; set; }

        public Uri Uri { get; set; }

        public HttpContent Content { get; set; }

        public SentHttpRequest(HttpMethod httpMethod, Uri uri, HttpContent content)
        {
            HttpMethod = httpMethod;
            Uri = uri;
            Content = content;
        }
    }
}
