namespace TradingBot.Exchanges.Abstractions
{
    public enum ExchangeState
    {
        Initializing,
        Connecting,
        ReconnectingAfterError,
        Connected,
        ReceivingPrices,
        ExecuteOrders,
        ErrorState,
        Stopped,
        Stopping
    }
}