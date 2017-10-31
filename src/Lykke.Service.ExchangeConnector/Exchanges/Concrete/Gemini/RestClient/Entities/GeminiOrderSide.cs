using System.Runtime.Serialization;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal enum GeminiOrderSide
    {
        [EnumMember(Value = "buy")]
        Buy,
        [EnumMember(Value = "sell")]
        Sell
    }
}
