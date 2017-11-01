using Newtonsoft.Json;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    internal class EventMessageResponse: EventResponse
    {
        
        [JsonProperty("code")]
        public Code Code { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

    internal sealed class ErrorEventMessageResponse : EventMessageResponse
    {

    }
}
