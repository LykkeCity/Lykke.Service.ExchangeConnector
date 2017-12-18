using System;
using System.Linq;

namespace Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model
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
