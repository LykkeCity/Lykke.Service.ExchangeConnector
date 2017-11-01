using Newtonsoft.Json.Linq;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    internal sealed class HeartbeatResponse
    {
        public long ChannelId { get; set; }

        public static HeartbeatResponse Parse(string json)
        {
            var arr = JArray.Parse(json);

            if (arr.Count != 2 || !(arr[0].Type == JTokenType.Integer && arr[1].Type == JTokenType.String))
            {
                return null;
            }
            var id = arr[0].Value<long>();

            return new HeartbeatResponse
            {
                ChannelId = id
            };
        }
    }
}
