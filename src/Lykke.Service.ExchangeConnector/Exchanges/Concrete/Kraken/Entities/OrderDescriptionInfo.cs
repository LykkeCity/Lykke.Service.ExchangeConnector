namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    public class OrderDescriptionInfo
    {
        public string Pair { get; set; }
        
        public TradeDirection Type { get; set; }
        
        public OrderType OrderType { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal Price2 { get; set; }
        
        public string Leverage { get; set; }
        
        public string Order { get; set; }
        
        public string Close { get; set; }
    }
}
