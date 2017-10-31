using System;
using Newtonsoft.Json;
using TradingBot.Exchanges.Abstractions.Models;
using TradingBot.Helpers;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal class GeminiPostContentBase: PostContentBase
    {
        [JsonProperty("nonce")]
        public int Nonce { get; set; }

        public GeminiPostContentBase()
        {
            Nonce = DateTime.UtcNow.ToUnixTimestampInt();
        }
    }
}
