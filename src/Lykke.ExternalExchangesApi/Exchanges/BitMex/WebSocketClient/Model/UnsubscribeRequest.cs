using System;
using System.Linq;

namespace TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model
{
    internal class UnsubscribeRequest : SubscribeRequest
    {
        public new static UnsubscribeRequest BuildRequest(params Tuple<string, string>[] filter)
        {
            return new UnsubscribeRequest
            {
                Operation = "unsubscribe",
                Arguments = filter.Select(f => $"{f.Item1}:{f.Item2}").ToArray()
            };
        }
    }
}
