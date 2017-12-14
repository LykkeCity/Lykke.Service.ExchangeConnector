using System;
using System.Threading.Tasks;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal interface IBitmexSocketSubscriber : IDisposable
    {
        void Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler);
        void Start();
        void Stop();
    }
}
