using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class SubscribeOrderBooksRequest : SubscribeChannelRequest
    {
        [JsonProperty("freq")]
        public string Freq { get; set; }

        [JsonProperty("prec")]
        public string Prec { get; set; }

        public static SubscribeOrderBooksRequest BuildRequest(string pair, string freq, string prec)
        {
            return new SubscribeOrderBooksRequest
            {
                Event = "subscribe",
                Pair = pair,
                Channel = WsChannel.book,
                Freq = freq,
                Prec = prec
            };
        }

    }
}