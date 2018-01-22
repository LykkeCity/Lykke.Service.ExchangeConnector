namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
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
