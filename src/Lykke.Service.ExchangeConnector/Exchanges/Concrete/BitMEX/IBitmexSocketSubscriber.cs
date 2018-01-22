using System;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal interface IBitmexSocketSubscriber : IDisposable
    {
        void Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler);
        void Start();
        void Stop();
    }
}
