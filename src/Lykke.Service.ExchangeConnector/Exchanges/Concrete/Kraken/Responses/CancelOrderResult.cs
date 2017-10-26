namespace TradingBot.Exchanges.Concrete.Kraken.Responses
{
    public class CancelOrderResult
    {
        /// <summary>
        /// Number of orders canceled
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// If set, order(s) is/are pending cancellation
        /// </summary>
        public bool Pending { get; set; }
    }
}