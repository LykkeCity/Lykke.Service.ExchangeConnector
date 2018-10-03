using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.ExchangeConnector.Client.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TradeRequestModality : byte
    {
        [EnumMember(Value = "Unspecified")]
        Unspecified = 0,
        [EnumMember(Value = "Liquidation")]
        Liquidation = 76,
        [EnumMember(Value = "Regular")]
        Regular = 82
    }
}
