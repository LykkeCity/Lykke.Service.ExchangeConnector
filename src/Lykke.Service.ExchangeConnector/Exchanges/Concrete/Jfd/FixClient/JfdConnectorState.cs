namespace TradingBot.Exchanges.Concrete.Jfd.FixClient
{
    internal enum JfdConnectorState
    {
        NotConnected,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        Error
    }
}
