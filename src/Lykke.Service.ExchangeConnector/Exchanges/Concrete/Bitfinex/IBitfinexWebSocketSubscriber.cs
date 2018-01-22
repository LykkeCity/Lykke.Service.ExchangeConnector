using System;
using System.Threading.Tasks;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal interface IBitfinexWebSocketSubscriber
    {
        Task Subscribe(Func<dynamic, Task> handlerFunc);
        void Start();
        void Stop();
    }
}
