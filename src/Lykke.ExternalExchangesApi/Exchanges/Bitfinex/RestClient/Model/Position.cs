using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
    public sealed class Position
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("base")]
        public decimal Base { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }

        [JsonProperty("swap")]
        public decimal Swap { get; set; }

        [JsonProperty("pl")]
        public decimal Pl { get; set; }

        public override string ToString()
        {
            var str = $"Id: {Id}, Symbol: {Symbol}, Status: {Status}, Base: {Base}, Amount: {Amount}, Timestamp: {Timestamp}" + $"Swap: {Swap}, Pl: {Pl}";
            return str;
        }
    }
}
