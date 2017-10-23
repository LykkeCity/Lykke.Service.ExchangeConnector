using System.Runtime.Serialization;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Entities
{
    internal enum GdaxOrderSide
    {
        [EnumMember(Value = "buy")]
        Buy,
        [EnumMember(Value = "sell")]
        Sell
    }
}
