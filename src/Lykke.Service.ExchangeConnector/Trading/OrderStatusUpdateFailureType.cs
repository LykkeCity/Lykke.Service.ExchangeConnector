namespace TradingBot.Trading
{
    public enum OrderStatusUpdateFailureType
    {
        None,
        Unknown,
        ExchangeError,
        ConnectorError,
        InsufficientFunds
    }
}
