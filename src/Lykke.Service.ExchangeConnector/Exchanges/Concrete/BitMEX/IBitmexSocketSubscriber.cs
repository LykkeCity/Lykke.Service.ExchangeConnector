using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    interface IBitmexSocketSubscriber : IDisposable
    {
        IBitmexSocketSubscriber Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler);
        void Start();
        void Stop();
    }
}
