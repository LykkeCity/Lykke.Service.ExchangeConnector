using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
    public sealed class MarginInfo
    {

        [JsonProperty("margin_balance")]
        public decimal MarginBalance { get; set; }

        [JsonProperty("unrealized_pl")]
        public decimal UnrealizedPl { get; set; }

        [JsonProperty("unrealized_swap")]
        public decimal UnrealizedSwap { get; set; }

        [JsonProperty("net_value")]
        public decimal NetValue { get; set; }

        [JsonProperty("required_margin")]
        public decimal RequiredMargin { get; set; }

        [JsonProperty("margin_limits")]
        public MarginLimit[] MarginLimits { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public override string ToString()
        {
            var str = $"MarginBalance: {MarginBalance},  UnrealizedPl: {UnrealizedPl} UnrealizedSwap: {UnrealizedSwap}, NetValue: {NetValue}, RequiredMargin: {RequiredMargin} Message: {Message}";
            return str;
        }
    }
}
