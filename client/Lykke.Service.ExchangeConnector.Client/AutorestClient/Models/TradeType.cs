// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.ExchangeConnector.Client.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines values for TradeType.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TradeType
    {
        [EnumMember(Value = "Unknown")]
        Unknown,
        [EnumMember(Value = "Buy")]
        Buy,
        [EnumMember(Value = "Sell")]
        Sell
    }
    internal static class TradeTypeEnumExtension
    {
        internal static string ToSerializedValue(this TradeType? value)
        {
            return value == null ? null : ((TradeType)value).ToSerializedValue();
        }

        internal static string ToSerializedValue(this TradeType value)
        {
            switch( value )
            {
                case TradeType.Unknown:
                    return "Unknown";
                case TradeType.Buy:
                    return "Buy";
                case TradeType.Sell:
                    return "Sell";
            }
            return null;
        }

        internal static TradeType? ParseTradeType(this string value)
        {
            switch( value )
            {
                case "Unknown":
                    return TradeType.Unknown;
                case "Buy":
                    return TradeType.Buy;
                case "Sell":
                    return TradeType.Sell;
            }
            return null;
        }
    }
}
