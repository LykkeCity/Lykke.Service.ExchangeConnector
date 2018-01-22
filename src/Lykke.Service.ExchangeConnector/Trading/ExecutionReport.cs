using System;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TradingBot.Trading
{
    public class ExecutionReport
    {
        /// <summary>
        /// A client assigned ID of the order
        /// </summary>
        public string ClientOrderId { get; internal set; }

        /// <summary>
        /// An exchange assigned ID of the order
        /// </summary>
        public string ExchangeOrderId { get; internal set; }

        /// <summary>
        /// An instrument description
        /// </summary>
        public Instrument Instrument { get; internal set; }

        /// <summary>
        /// A trade direction
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeType Type { get; internal set; }

        /// <summary>
        /// Transaction time
        /// </summary>
        public DateTime Time { get; internal set; }

        /// <summary>
        /// An actual price of the execution or order
        /// </summary>
        public decimal Price { get; internal set; }

        /// <summary>
        /// Trade volume
        /// </summary>
        public decimal Volume { get; internal set; }

        /// <summary>
        /// Execution fee
        /// </summary>
        public decimal Fee { get; internal set; }

        /// <summary>
        /// Fee currency
        /// </summary>
        public string FeeCurrency { get; internal set; }

        /// <summary>
        /// Indicates that operation was successful
        /// </summary>
        public bool Success { get; internal set; }

        /// <summary>
        /// Current status of the order
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderExecutionStatus ExecutionStatus { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatusUpdateFailureType FailureType { get; internal set; }

        /// <summary>
        /// An arbitrary message from the exchange related to the execution|order 
        /// </summary>
        public string Message { get; internal set; }

        /// <summary>
        /// A type of the order
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType OrderType { get; internal set; }

        /// <summary>
        /// A type of the execution. ExecType = Trade means it is an execution, otherwise it is an order
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecType ExecType { get; internal set; }

        public ExecutionReport()
        {

        }

        [JsonConstructor]
        public ExecutionReport(Instrument instrument, DateTime time, decimal price,
            decimal volume, TradeType type, string orderId, OrderExecutionStatus executionStatus)
        {
            Instrument = instrument;
            Time = time;
            Price = price;
            Volume = volume;
            Type = type;
            Fee = 0; // TODO
            ExchangeOrderId = orderId;
            ExecutionStatus = executionStatus;
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
