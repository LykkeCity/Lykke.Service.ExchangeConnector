using System.Runtime.Serialization;

namespace TradingBot.Exchanges.Concrete.Gemini.RestClient.Entities
{
    internal enum GeminiOrderType
    {
        [EnumMember(Value = "limit")]
        Limit,
        [EnumMember(Value = "market")]
        Market,
        [EnumMember(Value = "stop")]
        Stop
    }
}
