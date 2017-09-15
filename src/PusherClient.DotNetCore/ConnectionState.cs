namespace PusherClient.DotNetCore
{
    public enum ConnectionState
    {
        Initialized,
        Connecting,
        Connected,
        Disconnected,
        WaitingToReconnect
    }
}