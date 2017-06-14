using System;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TradingBot.Common.Trading;

namespace TradingBot
{
    public class PriceTableEntity : TableEntity // TODO: move to TradingBot.Common
    {
        public PriceTableEntity()
        {
        }

        public PriceTableEntity(string assetId, DateTime minute)
            : base(assetId, minute.ToString("yyyy-MM-dd HH:mm:ss"))
        {
        }

        public PriceTableEntity(string assetId, DateTime minute, string serializedPrices) : this(assetId, minute)
        {
            SerializedPrices = serializedPrices;
        }

        public PriceTableEntity(string assetId, DateTime minute, TickPrice[] prices) : this(assetId, minute)
        {
            SerializedPrices = JsonConvert.SerializeObject(prices, new JsonSerializerSettings() { DateFormatString = "yyyy-MM-ddTHH:mm:ss.fff" });
        }

        public string SerializedPrices { get; set; }
    }
}
