namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class PingRequest : SubscribeRequest
    {
        public PingRequest()
        {
            Event = "ping";
        }
    }
}
