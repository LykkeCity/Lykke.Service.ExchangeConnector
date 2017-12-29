using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.ExternalExchangesApi.Shared;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using TradingBot.Exchanges.Concrete.Shared;
using TradingBot.Infrastructure.Configuration;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    /// <summary>
    /// Redirects calls to opened-data socket and authorized socket.
    /// </summary>
    internal sealed class BitmexSocketSubscriberDecorator : IBitmexSocketSubscriber
    {
        private readonly IMessenger<object, string> _openMessenger;
        private readonly IMessenger<object, string> _authMessenger;
        private readonly BitmexSocketSubscriber _openSocket;
        private readonly BitmexSocketSubscriber _authSocket;

        public BitmexSocketSubscriberDecorator(BitMexExchangeConfiguration configuration, ILog log)
        {
            _openMessenger = new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log);
            _authMessenger = new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log);
            _openSocket = new BitmexSocketSubscriber(_openMessenger, configuration, log, false);
            _authSocket = new BitmexSocketSubscriber(_authMessenger, configuration, log, true);
        }

        public void Subscribe(BitmexTopic topic, Func<TableResponse, Task> topicHandler)
        {
            switch (topic)
            {
                case BitmexTopic.order:
                case BitmexTopic.execution:
                    _authSocket.Subscribe(topic, topicHandler);
                    return;
                case BitmexTopic.orderBookL2:
                case BitmexTopic.quote:
                    _openSocket.Subscribe(topic, topicHandler);
                    return;
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
            _openMessenger.Dispose();
            _authMessenger.Dispose();
        }
    }
}
