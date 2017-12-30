namespace TradingBot.Trading
{
    public enum OrderExecutionStatus
    {
        Unknown,
        Fill,
        PartialFill,
        Cancelled,
        Rejected,
        New,
        Pending
    }
}
