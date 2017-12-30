using System;
using Lykke.ExternalExchangesApi.Shared;
using Newtonsoft.Json.Linq;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class TradeExecutionUpdate
    {
        public long ChannelId { get; set; }

        public string Seq { get; set; }

        public long Id { get; set; }

        public string AssetPair { get; set; }

        public DateTime TimeStamp { get; set; }

        public long OrderId { get; set; }

        public decimal Volume { get; set; }

        public decimal Price { get; set; }

        public string OrderType { get; set; }

        public decimal? OrderPrice { get; set; }

        public decimal Fee { get; set; }

        public string FeeCurrency { get; set; }

        public static TradeExecutionUpdate Parse(string json)
        {
            if (JToken.Parse(json).Type != JTokenType.Array)
                return null;

            var arr = JArray.Parse(json);

            if (!(arr[0].Type == JTokenType.Integer &&
                  arr[1].Type == JTokenType.String &&
                  arr[2].Type == JTokenType.Array))

            {
                return null;
            }

            if (arr[1].Value<string>() != @"tu")
            {
                return null;
            }

            var item = arr[2].Value<JArray>();

            return new TradeExecutionUpdate
            {
                ChannelId = arr[0].Value<long>(),
                Seq = item[0].Value<string>(),
                Id = item[1].Value<long>(),
                AssetPair = item[2].Value<string>(),
                TimeStamp = UnixTimeConverter.FromUnixTime(item[3].Value<long>()),
                OrderId = item[4].Value<long>(),
                Volume = item[5].Value<decimal>(),
                Price = item[6].Value<decimal>(),
                OrderType = item[7].Value<string>(),
                OrderPrice = item[8].Value<decimal?>(),
                Fee = item[9].Value<decimal>(),
                FeeCurrency = item[10].Value<string>()
            };
        }
    }
}
