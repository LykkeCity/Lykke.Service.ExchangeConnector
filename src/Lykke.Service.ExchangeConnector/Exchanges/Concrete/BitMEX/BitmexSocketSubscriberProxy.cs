using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    /// <summary>
    /// Redirects calls to opened-data socket and authorized socket.
    /// </summary>
    sealed class BitmexSocketSubscriberProxy : IBitmexSocketSubscriber
    {
        private readonly BitmexSocketSubscriber _openSocket;
        private readonly BitmexSocketSubscriber _authSocket;

        public BitmexSocketSubscriberProxy(BitMexExchangeConfiguration configuration, ILog log)
        {
            _openSocket = new BitmexSocketSubscriber(configuration, log, false);
            _authSocket = new BitmexSocketSubscriber(configuration, log, true);
        }

        public IBitmexSocketSubscriber Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler)
        {
            switch (topic)
            {
                case BitmexTopic.Order:
                case BitmexTopic.Execution:
                    _authSocket.Subscribe(topic, topicHandler);
                    return this;
                case BitmexTopic.OrderBookL2:
                case BitmexTopic.Quote:
                    _openSocket.Subscribe(topic, topicHandler);
                    return this;
                default:
                    throw new InvalidOperationException($"Unexpected topic '{topic}' on subscribe.");
            }
        }

        public void Start()
        {
            _openSocket.Start();
            _authSocket.Start();
        }

        public void Stop()
        {
            _openSocket.Stop();
            _authSocket.Stop();
        }

        public void Dispose()
        {
            _openSocket.Dispose();
            _authSocket.Dispose();
        }
    }
}
