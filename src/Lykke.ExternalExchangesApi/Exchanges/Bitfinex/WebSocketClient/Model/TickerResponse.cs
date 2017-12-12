using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model
{
    public sealed class TickerResponse
    {
        public long ChannelId { get; private set; }

        [JsonProperty("BID")]
        public decimal Bid { get; private set; }

        [JsonProperty("BID_SIZE")]
        public decimal BidSize { get; private set; }

        [JsonProperty("ASK")]
        public decimal Ask { get; private set; }

        [JsonProperty("ASK_SIZE")]
        public decimal AskSize { get; private set; }

        [JsonProperty("DAILY_CHANGE")]
        public decimal DailyChange { get; private set; }

        [JsonProperty("DAILY_CHANGE_PERC")]
        public decimal DailyChangePerc { get; private set; }

        [JsonProperty("LAST_PRICE")]
        public decimal LastPrice { get; private set; }

        [JsonProperty("VOLUME")]
        public decimal Volume { get; private set; }

        [JsonProperty("HIGH")]
        public decimal High { get; private set; }

        [JsonProperty("LOW")]
        public decimal Low { get; private set; }

        public static TickerResponse Parse(string json)
        {
            if (JToken.Parse(json).Type != JTokenType.Array)
                return null;

            var arr = JArray.Parse(json);

            if (arr.Count != 11 || 
                !(arr[0].Type == JTokenType.Integer &&
                  (arr[1].Type == JTokenType.Float || arr[1].Type == JTokenType.Integer) &&
                  (arr[2].Type == JTokenType.Float || arr[2].Type == JTokenType.Integer) &&
                  (arr[3].Type == JTokenType.Float || arr[3].Type == JTokenType.Integer) &&
                  (arr[4].Type == JTokenType.Float || arr[4].Type == JTokenType.Integer) &&
                  (arr[5].Type == JTokenType.Float || arr[5].Type == JTokenType.Integer) &&
                  (arr[6].Type == JTokenType.Float || arr[6].Type == JTokenType.Integer) &&
                  (arr[7].Type == JTokenType.Float || arr[7].Type == JTokenType.Integer) &&
                  (arr[8].Type == JTokenType.Float || arr[8].Type == JTokenType.Integer) &&
                  (arr[9].Type == JTokenType.Float || arr[9].Type == JTokenType.Integer) &&
                  (arr[10].Type == JTokenType.Float || arr[10].Type == JTokenType.Integer)))
            {
                return null;
            }

            return new TickerResponse
            {
                ChannelId = arr[0].Value<long>(),
                Bid = arr[1].Value<decimal>(),
                BidSize = arr[2].Value<decimal>(),
                Ask = arr[3].Value<decimal>(),
                AskSize = arr[4].Value<decimal>(),
                DailyChange = arr[5].Value<decimal>(),
                DailyChangePerc = arr[6].Value<decimal>(),
                LastPrice = arr[7].Value<decimal>(),
                Volume = arr[8].Value<decimal>(),
                High = arr[9].Value<decimal>(),
                Low = arr[10].Value<decimal>()
            };
        }
    }
}
