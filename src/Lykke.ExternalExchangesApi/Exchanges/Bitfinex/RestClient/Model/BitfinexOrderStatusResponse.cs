//using Newtonsoft.Json;

//namespace TradingBot.Exchanges.Concrete.Bitfinex.RestClient.Model
//{

//    internal sealed class BitfinexOrderStatusResponse
//    {
//        [JsonProperty("id")]
//        public int Id { get; set; }

//        [JsonProperty("symbol")]
//        public string Symbol { get; set; }

//        [JsonProperty("exchange")]
//        public string Exchange { get; set; }

//        [JsonProperty("price")]
//        public decimal Price { get; set; }

//        [JsonProperty("avg_execution_price")]
//        public decimal AvgExecutionPrice { get; set; }

//        [JsonProperty("side")]
//        public string Side { get; set; }

//        [JsonProperty("type")]
//        public string Type { get; set; }

//        [JsonProperty("timestamp")]
//        public string Timestamp { get; set; }

//        [JsonProperty("is_live")]
//        public bool IsLive { get; set; }

//        [JsonProperty("is_cancelled")]
//        public bool IsCancelled { get; set; }

//        [JsonProperty("was_forced")]
//        public bool WasForced { get; set; }

//        [JsonProperty("original_amount")]
//        public decimal OriginalAmount { get; set; }

//        [JsonProperty("remaining_amount")]
//        public decimal RemainingAmount { get; set; }

//        [JsonProperty("executed_amount")]
//        public decimal ExecutedAmount { get; set; }

//        public override string ToString()
//        {
//           var str = $"(Id: {Id}) Symb:{Symbol} {Side} Sz:{OriginalAmount} - Px:{Price}. (Type:{Type}, IsLive:{IsLive}, Executed Amt:{ExecutedAmount})" + $"(IsCancelled: {IsCancelled}, WasForced: {WasForced}, RemainingAmount: {RemainingAmount}, ExecutedAmount: {ExecutedAmount})";
//           return str;
//        }
//    }

//}

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.RestClient.Model
{
}