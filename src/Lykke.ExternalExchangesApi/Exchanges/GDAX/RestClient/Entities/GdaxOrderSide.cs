using System.Runtime.Serialization;

namespace Lykke.ExternalExchangesApi.Exchanges.GDAX.RestClient.Entities
{
    public enum GdaxOrderSide
    {
        [EnumMember(Value = "buy")]
        Buy,
        [EnumMember(Value = "sell")]
        Sell
    }
}
