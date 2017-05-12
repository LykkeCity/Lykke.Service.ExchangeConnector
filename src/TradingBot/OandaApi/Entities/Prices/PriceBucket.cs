namespace TradingBot.OandaApi.Entities.Prices
{
    /// <summary>
    /// A Price Bucket represents a price available for an amount of liquidity.
    /// </summary>
    public class PriceBucket
    {
        /// <summary>
        /// The Price offered by the PriceBucket.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// The amount of liquidity offered by the PriceBucket.
        /// </summary>
        public int Liquidity { get; set; }
    }
}
