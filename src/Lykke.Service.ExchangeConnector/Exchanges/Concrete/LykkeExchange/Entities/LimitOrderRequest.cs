using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class LimitOrderRequest
    {
        public string AssetPairId { get; set; }
        
        public TradeType OrderAction { get; set; }
        
        public decimal Volume { get; set; }
        
        public decimal Price { get; set; }
    }
}