using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Trading
{
    public class OrderStatusUpdate
    {
        public string ClientOrderId { get; internal set; }

        public string ExchangeOrderId { get; internal set; }

        public bool Success { get; internal set; }

        public string Exchange { get; internal set; }

        public Instrument Instrument { get; internal set; }

        public string InstrumentName { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TradeType Type { get; internal set; }

        public DateTime Time { get; internal set; }

        public decimal Price { get; internal set; }

        public decimal Volume { get; internal set; }

        public decimal Fee { get; internal set; }

        public string OrderId { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderExecutionStatus Status { get; internal set; }

        public OrderStatusUpdateFailureType FailureType { get; internal set; }

        public string Message { get; set; }

        [JsonConstructor]
        public OrderStatusUpdate()
        {
            
        }

        [JsonConstructor]
        public OrderStatusUpdate(Instrument instrument, DateTime time, decimal price, 
            decimal volume, TradeType type, string orderId, OrderExecutionStatus status)
        {
            Instrument = instrument;
            Time = time;
            Price = price;
            Volume = volume;
            Type = type;
            Fee = 0; // TODO
            OrderId = orderId;
            Status = status;
        }

        public override string ToString()
        {
            return $"ClientId: {ClientOrderId}, ExternalId: {ExchangeOrderId}, Success: {Success}, {Exchange}, {InstrumentName}";
        }
    }
}
