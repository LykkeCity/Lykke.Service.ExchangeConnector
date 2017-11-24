using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderRequest
    {
        public string AssetPairId { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeType OrderAction { get; set; }
        
        public decimal Volume { get; set; }
        
        public decimal Price { get; set; }
    }
}
