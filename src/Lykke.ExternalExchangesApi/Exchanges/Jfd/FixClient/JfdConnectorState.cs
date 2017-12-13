namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    public enum JfdConnectorState
    {
        NotConnected,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        Error
    }
}
