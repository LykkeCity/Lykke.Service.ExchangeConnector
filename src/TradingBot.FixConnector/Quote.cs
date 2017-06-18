namespace TradingBot.FixConnector
{
    public class Quote
    {
        public decimal Price { get; }
        public decimal Volume { get; }
        public bool IsBuy { get; }

        public Quote(decimal price, decimal volume, bool isBuy)
        {
            IsBuy = isBuy;
            Volume = volume;
            Price = price;
        }
    }
}
