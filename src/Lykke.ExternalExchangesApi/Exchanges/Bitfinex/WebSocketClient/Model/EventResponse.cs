using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public abstract class EventResponse
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
                case PongResponse.Tag:
                    response = JsonConvert.DeserializeObject<PongResponse>(json);
                    break;
                case "auth":
                    response = JsonConvert.DeserializeObject<AuthMessageResponse>(json);
                    break;
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
