using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.ExternalExchangesApi.Exchanges.Bitfinex.WebSocketClient.Model;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;

namespace TradingBot.Exchanges.Concrete.Bitfinex
{
    internal sealed class BitfinexExecutionHarvester : IStartable, IStopable
    {
        private readonly IBitfinexWebSocketSubscriber _socketSubscriber;
        private readonly BitfinexModelConverter _bitfinexModelConverter;
        private readonly IHandler<ExecutionReport> _handler;
        private readonly ILog _log;

        public BitfinexExecutionHarvester(IBitfinexWebSocketSubscriber socketSubscriber, BitfinexExchangeConfiguration configuration, IHandler<ExecutionReport> handler, ILog log)
        {
            _socketSubscriber = socketSubscriber;
            _bitfinexModelConverter = new BitfinexModelConverter(configuration.SupportedCurrencySymbols);
            _handler = handler;
            _log = log;
        }

        private Task MessageHandler(TradeExecutionUpdate tradeUpdate)
        {
            var execution = _bitfinexModelConverter.ToOrderStatusUpdate(tradeUpdate);
            return _handler.Handle(execution);
        }

        private Task MessageDispatcher(dynamic message)
        {
            return MessageHandler(message);
        }

        public void Start()
        {
            _socketSubscriber.Subscribe(MessageDispatcher);
            _socketSubscriber.Start();
            _log.WriteInfoAsync(GetType().Name, "Initialization", "Started");
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _socketSubscriber.Stop();
            _log.WriteInfoAsync(GetType().Name, "Cleanup", "Stopped");
        }
    }
}
