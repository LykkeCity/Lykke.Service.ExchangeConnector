using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

}
