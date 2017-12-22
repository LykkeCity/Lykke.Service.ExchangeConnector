using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class OrderBookSnapshotResponse
    {
        public long ChannelId { get; }

        public IReadOnlyCollection<OrderBookItemResponse> Orders { get; }

        private OrderBookSnapshotResponse(long channelId, IReadOnlyCollection<OrderBookItemResponse> orders)
        {
            ChannelId = channelId;
            Orders = orders;
        }


        public static OrderBookSnapshotResponse Parse(string json)
        {
            if (JToken.Parse(json).Type != JTokenType.Array)
            {
                return null;
            }
            var arr = JArray.Parse(json);

            if (arr.Count != 2 || !(arr[0].Type == JTokenType.Integer && arr[1].Type == JTokenType.Array))
            {
                return null;
            }
            var id = arr[0].Value<long>();
            var orders = arr[1].Select(t => new OrderBookItemResponse
            {
                Id = t[0].Value<long>(),
                Price = t[1].Value<decimal>(),
                Amount = t[2].Value<decimal>()
            }).ToArray();
            return new OrderBookSnapshotResponse(id, orders);
        }
    }
}
