using System;
using System.Threading.Tasks;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal interface IBitfinexSocketSubscriber : IDisposable
    {
        void Subscribe(WsChannel topic, Func<object, Task> topicHandler);
        void Start();
        void Stop();
    }
}
