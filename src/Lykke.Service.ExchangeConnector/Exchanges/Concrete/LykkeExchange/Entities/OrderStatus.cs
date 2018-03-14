namespace TradingBot.Exchanges.Concrete.LykkeExchange.Entities
{
    public enum OrderStatus
    {
        Pending,
        InOrderBook,
        Processing,
        Matched,
        NotEnoughFunds,
        NoLiquidity,
        UnknownAsset,
        Cancelled,
        LeadToNegativeSpread,
        InvalidFee
    }
}
