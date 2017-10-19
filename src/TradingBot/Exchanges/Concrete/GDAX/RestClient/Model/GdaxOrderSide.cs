using System.Runtime.Serialization;

namespace TradingBot.Exchanges.Concrete.GDAX.RestClient.Model
{
    internal enum GdaxOrderSide
    {
        [EnumMember(Value = "buy")]
        Buy,
        [EnumMember(Value = "sell")]
        Sell
    }
}
