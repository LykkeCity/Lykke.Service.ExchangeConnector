namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public enum Code
    {
        None = 0,
        SubscriptionFailed = 10300,
        AlreadySubscribed = 10301,
        UnknownChannel = 10302,
        UnsubscriptionFailed = 10400,
        NotSubsribed = 10401,
        RestartServer = 20051,
        Pause = 20060,
        Resume = 20061,
        InvalidApiKey = 10100
    }
}
