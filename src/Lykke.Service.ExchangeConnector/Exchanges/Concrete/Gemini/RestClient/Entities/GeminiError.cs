using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal sealed class GeminiError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

}
