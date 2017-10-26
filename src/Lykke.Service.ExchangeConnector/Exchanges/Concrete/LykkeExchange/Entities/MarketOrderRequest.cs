using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class MarketOrderRequest
    {
        public string AssetPairId { get; set; }
        
        public TradeType OrderAction { get; set; }
        
        public decimal Volume { get; set; }
    }
}