using System.Runtime.Serialization;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal enum GdaxOrderType
    {
        [EnumMember(Value = "limit")]
        Limit,
        [EnumMember(Value = "market")]
        Market,
        [EnumMember(Value = "stop")]
        Stop
    }
}
