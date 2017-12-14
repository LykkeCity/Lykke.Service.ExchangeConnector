//using System;
//using System.Threading.Tasks;
//using Common.Log;
//using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient;
//using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
//using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
//using Lykke.ExternalExchangesApi.Shared;
//using TradingBot.Exchanges.Concrete.BitMEX;
//using TradingBot.Infrastructure.Configuration;
//
//namespace TradingBot.Exchanges.Concrete.Bitfinex
//{
//    /// <summary>
//    /// Redirects calls to opened-data socket and authorized socket.
//    /// </summary>
//    internal sealed class BitfinexSocketSubscriberDecorator : IBitfinexSocketSubscriber
//    {
//        private readonly IMessenger<object, string> _openMessenger;
//        private readonly IMessenger<object, string> _authMessenger;
//        private readonly BitfinexSocketSubscriber _openSocket;
//        private readonly BitfinexSocketSubscriber _authSocket;
//
//        public BitfinexSocketSubscriberDecorator(BitMexExchangeConfiguration configuration, ILog log)
//        {
//            _openMessenger = new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log);
//            _authMessenger = new WebSocketTextMessenger(configuration.WebSocketEndpointUrl, log);
//            _openSocket = new BitfinexSocketSubscriber(_openMessenger, configuration, log, false);
//            _authSocket = new BitfinexSocketSubscriber(_authMessenger, configuration, log, true);
//        }
//
//        public void Subscribe(WsChannel topic, Func<object, Task> topicHandler)
//        {
//            switch (topic)
//            {
////                case BitmexTopic.order:
////                case BitmexTopic.execution:
////                    _authSocket.Subscribe(topic, topicHandler);
////                    return;
//                case WsChannel.ticker:
//                case WsChannel.book:
//                    _openSocket.Subscribe(topic, topicHandler);
//                    return;
//                default:
//                    throw new InvalidOperationException($"Unexpected topic '{topic}' on subscribe.");
//            }
//        }
//
//        public void Start()
//        {
//            _openSocket.Start();
//            _authSocket.Start();
//        }
//
//        public void Stop()
//        {
//            _openSocket.Stop();
//            _authSocket.Stop();
//        }
//
//        public void Dispose()
//        {
//            _openSocket.Dispose();
//            _authSocket.Dispose();
//            _openMessenger.Dispose();
//            _authMessenger.Dispose();
//        }
//    }
//}
