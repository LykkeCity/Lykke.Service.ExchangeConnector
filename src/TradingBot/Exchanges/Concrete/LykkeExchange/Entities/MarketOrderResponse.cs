namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public class MarketOrderResponse
    {
        /// <summary>
        /// average execution price
        /// </summary>
        public decimal Result { get; set; }
        
        public ErrorModel Error { get; set; }
    }
}