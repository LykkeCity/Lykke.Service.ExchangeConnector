using Newtonsoft.Json.Linq;

namespace TradingBot.Exchanges.Concrete.Bitfinex.WebSocketClient.Model
{
    internal sealed class OrderBookUpdateResponse : OrderBookItemResponse
    {
        public long ChannelId { get; private set; }

        public static OrderBookUpdateResponse Parse(string json)
        {
            if (JToken.Parse(json).Type != JTokenType.Array)
            {
                return null;
            }
            var arr = JArray.Parse(json);
            if (arr.Count != 4 || !(arr[0].Type == JTokenType.Integer && arr[1].Type == JTokenType.Integer))
            {
                return null;
            }

            return new OrderBookUpdateResponse
            {
                ChannelId = arr[0].Value<long>(),
                Id = arr[1].Value<long>(),
                Price = arr[2].Value<decimal>(),
                Amount = arr[3].Value<decimal>()
            };
        }
    }
}
