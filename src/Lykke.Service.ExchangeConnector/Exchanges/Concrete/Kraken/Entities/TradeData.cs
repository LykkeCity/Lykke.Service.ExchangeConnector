using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lykke.ExternalExchangesApi.Helpers;

namespace TradingBot.Exchanges.Concrete.Kraken.Entities
{
    [JsonConverter(typeof(TradesResultJsonConverter))]
    public class TradesResult : Dictionary<string, List<TradeData>>
    {
        public string Last { get; set; }
    }

    public class TradesResultJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TradesResult);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new TradesResult();
            
            while(reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    if (reader.Value.ToString() != "last")
                    {
                        string pair = reader.Value.ToString();
                        List<TradeData> trades = new List<TradeData>();

                        reader.Read();
                        reader.Read();

                        // Parse elements of the array
                        while (reader.TokenType == JsonToken.StartArray)
                        {
                            var trade = (TradeData)new TradeDataJsonConverter().ReadJson(reader, null, null, null);
                                
                            // Pull off next array in list, or end of list
                            reader.Read();

                            trades.Add(trade);
                        }

                        result.Add(pair, trades);
                    }
                    else
                    {
                        result.Last = reader.ReadAsString();
                    }
                }
            }
            
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class TradeDataJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TradeData);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var trade = new TradeData();

            trade.Price = reader.ReadAsDecimal() ?? 0;
            trade.Volume = reader.ReadAsDecimal() ?? 0;
            trade.Time = DateTimeUtils.FromUnix(reader.ReadAsDecimal() ?? 0m);
            trade.Direction = reader.ReadAsString() == "s" ? TradeDirection.Sell : TradeDirection.Buy;
            trade.Type = reader.ReadAsString() == "m" ? OrderType.Market : OrderType.Limit;
            trade.Miscellaneous = reader.ReadAsString();
            
            reader.Read(); // EndArray

            return trade;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonConverter(typeof(TradeDataJsonConverter))]
    public class TradeData
    {
        /*
             (<price>, <volume>, <time>, <buy/sell>, <market/limit>, <miscellaneous>)

        [
            "77.500000",
            "0.64516000",
            1494997079.6643,
            "s",
            "l",
            ""
        ]
        */

        public TradeData()
        {

        }

        public TradeData(object[] values)
        {
            if (values.Length < 6)
                throw new ArgumentException(nameof(values), "Must be at least 6 values");

            Price = decimal.Parse(values[0].ToString(), CultureInfo.InvariantCulture);
            Volume = decimal.Parse(values[1].ToString(), CultureInfo.InvariantCulture);
            Time = DateTimeUtils.FromUnix((double)values[2]);
            
        }

        public decimal Price { get; set; }

        public decimal Volume { get; set; }

        public DateTime Time { get; set; }

        public TradeDirection Direction { get; set; }

        public OrderType Type { get; set; }

        public string Miscellaneous { get; set; }
    }

    public enum TradeDirection
    {
        Buy,
        Sell
    }

    public enum OrderType
    {
        Market,
        Limit
    }
}
