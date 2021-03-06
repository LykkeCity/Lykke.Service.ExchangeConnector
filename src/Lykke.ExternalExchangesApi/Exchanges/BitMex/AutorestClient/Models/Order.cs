// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

using Microsoft.Rest;
using Newtonsoft.Json;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.AutorestClient.Models
{
    /// <summary>
    /// Placement, Cancellation, Amending, and History
    /// </summary>
    public partial class Order
    {
        /// <summary>
        /// Initializes a new instance of the Order class.
        /// </summary>
        public Order()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Order class.
        /// </summary>
        public Order(string orderID, string clOrdID = default(string), string clOrdLinkID = default(string), double? account = default(double?), string symbol = default(string), string side = default(string), double? simpleOrderQty = default(double?), double? orderQty = default(double?), double? price = default(double?), double? displayQty = default(double?), double? stopPx = default(double?), double? pegOffsetValue = default(double?), string pegPriceType = default(string), string currency = default(string), string settlCurrency = default(string), string ordType = default(string), string timeInForce = default(string), string execInst = default(string), string contingencyType = default(string), string exDestination = default(string), string ordStatus = default(string), string triggered = default(string), bool? workingIndicator = default(bool?), string ordRejReason = default(string), double? simpleLeavesQty = default(double?), double? leavesQty = default(double?), double? simpleCumQty = default(double?), double? cumQty = default(double?), double? avgPx = default(double?), string multiLegReportingType = default(string), string text = default(string), System.DateTime? transactTime = default(System.DateTime?), System.DateTime? timestamp = default(System.DateTime?))
        {
            OrderID = orderID;
            ClOrdID = clOrdID;
            ClOrdLinkID = clOrdLinkID;
            Account = account;
            Symbol = symbol;
            Side = side;
            SimpleOrderQty = simpleOrderQty;
            OrderQty = orderQty;
            Price = price;
            DisplayQty = displayQty;
            StopPx = stopPx;
            PegOffsetValue = pegOffsetValue;
            PegPriceType = pegPriceType;
            Currency = currency;
            SettlCurrency = settlCurrency;
            OrdType = ordType;
            TimeInForce = timeInForce;
            ExecInst = execInst;
            ContingencyType = contingencyType;
            ExDestination = exDestination;
            OrdStatus = ordStatus;
            Triggered = triggered;
            WorkingIndicator = workingIndicator;
            OrdRejReason = ordRejReason;
            SimpleLeavesQty = simpleLeavesQty;
            LeavesQty = leavesQty;
            SimpleCumQty = simpleCumQty;
            CumQty = cumQty;
            AvgPx = avgPx;
            MultiLegReportingType = multiLegReportingType;
            Text = text;
            TransactTime = transactTime;
            Timestamp = timestamp;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderID")]
        public string OrderID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clOrdID")]
        public string ClOrdID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "clOrdLinkID")]
        public string ClOrdLinkID { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "account")]
        public double? Account { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "side")]
        public string Side { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "simpleOrderQty")]
        public double? SimpleOrderQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "orderQty")]
        public double? OrderQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public double? Price { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "displayQty")]
        public double? DisplayQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "stopPx")]
        public double? StopPx { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "pegOffsetValue")]
        public double? PegOffsetValue { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "pegPriceType")]
        public string PegPriceType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "settlCurrency")]
        public string SettlCurrency { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ordType")]
        public string OrdType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "timeInForce")]
        public string TimeInForce { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "execInst")]
        public string ExecInst { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "contingencyType")]
        public string ContingencyType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "exDestination")]
        public string ExDestination { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ordStatus")]
        public string OrdStatus { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "triggered")]
        public string Triggered { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "workingIndicator")]
        public bool? WorkingIndicator { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ordRejReason")]
        public string OrdRejReason { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "simpleLeavesQty")]
        public double? SimpleLeavesQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "leavesQty")]
        public double? LeavesQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "simpleCumQty")]
        public double? SimpleCumQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "cumQty")]
        public double? CumQty { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "avgPx")]
        public double? AvgPx { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "multiLegReportingType")]
        public string MultiLegReportingType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "transactTime")]
        public System.DateTime? TransactTime { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public System.DateTime? Timestamp { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (OrderID == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "OrderID");
            }
        }
    }
}
