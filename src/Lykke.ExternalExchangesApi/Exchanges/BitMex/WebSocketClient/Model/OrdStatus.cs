namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    public enum OrdStatus
    {
        Unknown = 0,
        New,
        PartiallyFilled,
        Filled,
        Canceled,
        Rejected,
        Pending
    }
}
