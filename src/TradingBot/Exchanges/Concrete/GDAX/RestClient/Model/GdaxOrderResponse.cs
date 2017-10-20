using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal sealed class GdaxOrderResponse
    {
        //public override string ToString()
        //{
        //    var str = $"New Order (Id: {Id}) Symb:{Symbol} {Side} Sz:{OriginalAmount} - Px:{Price}. (Type:{Type}, IsLive:{IsLive}, Executed Amt:{ExecutedAmount} - OrderId: {Id})" + $"(IsCancelled: {IsCancelled}, WasForced: {WasForced}, RemainingAmount: {RemainingAmount}, ExecutedAmount: {ExecutedAmount})";
        //    return str;
        //}

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("side")]
        public GdaxOrderSide Side { get; set; }

        [JsonProperty("stp")]
        public string Stp { get; set; }

        [JsonProperty("type")]
        public GdaxOrderType OrderType { get; set; }

        [JsonProperty("time_in_force")]
        public string TimeInForce { get; set; }

        [JsonProperty("post_only")]
        public string PostOnly { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("fill_fees")]
        public decimal FillFees { get; set; }

        [JsonProperty("filled_size")]
        public decimal FilledSize { get; set; }

        [JsonProperty("executed_value")]
        public decimal ExecutedValue { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("settled")]
        public bool Settled { get; set; }
    }
}
