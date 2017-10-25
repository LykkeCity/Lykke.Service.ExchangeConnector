using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    internal abstract class EventResponse
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        public static EventResponse Parse(string json)
        {
            var token = JToken.Parse(json);
            if (token.Type == JTokenType.Array || token.All(t => t.Path != "event"))
            {
                return null;
            }

            var eventType = token["event"].Value<string>();
            EventResponse response = null;

            switch (eventType)
            {
                case "error":
                    response = JsonConvert.DeserializeObject<ErrorEventMessageResponse>(json);
                    break;
                case "info":
                    if (token.Any(t => t.Path == "code"))
                    {
                        response = JsonConvert.DeserializeObject<EventMessageResponse>(json);
                    }
                    else
                    {
                        response = JsonConvert.DeserializeObject<InfoResponse>(json);
                    }
                    break;
                case "subscribed":
                    response = JsonConvert.DeserializeObject<SubscribedResponse>(json);
                    break;
            }
            return response;
        }
    }
}
