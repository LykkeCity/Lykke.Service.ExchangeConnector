using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Newtonsoft.Json;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient;
using Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model;
using TradingBot.Handlers;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = Lykke.ExternalExchangesApi.Exchanges.BitMex.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexOrderHarvester : IStartable, IStopable
    {
        private readonly IBitmexSocketSubscriber _socketSubscriber;
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private readonly IHandler<OrderStatusUpdate> _ackHandler;
        private readonly IHandler<OrderStatusUpdate> _tradeHandler;

        public BitMexOrderHarvester(
            BitMexExchangeConfiguration configuration,
            IBitmexSocketSubscriber socketSubscriber,
            IHandler<OrderStatusUpdate> ackHandler,
            IHandler<OrderStatusUpdate> tradeHandler,
            ILog log)
        {
            _socketSubscriber = socketSubscriber;
            _log = log;
            _ackHandler = ackHandler;
            _tradeHandler = tradeHandler;
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols, BitMexExchange.Name);
        }



        private async Task HandleResponseAsync(TableResponse table)
        {
            if (_ackHandler == null || _tradeHandler == null)
            {
                throw new InvalidOperationException("Acknowledgement handler or executed trader is not set.");
            }

            if (!ValidateOrder(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexOrderHarvester), nameof(HandleResponseAsync),
                    $"Ignoring invalid 'order' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            switch (table.Action)
            {
                case Action.Insert:
                    var acks = table.Data.Select(row => _mapper.OrderToAck(row));
                    foreach (var ack in acks)
                    {
                        await _ackHandler.Handle(ack);
                    }
                    break;
                case Action.Update:
                    foreach (var row in table.Data)
                    {
                        var trade = _mapper.OrderToTrade(row);

                        if (trade.ExecutionStatus == OrderExecutionStatus.Unknown)
                        {
                            await _log.WriteWarningAsync(nameof(BitMexOrderHarvester), nameof(HandleResponseAsync),
                                $"Can't convert trade status {row.OrdStatus} into ExecutionStatus. Converted item: {trade}. Don't call handlers.");
                        }
                        else
                        {
                            await _tradeHandler.Handle(trade);
                        }
                    }
                    break;
            }
        }

        private bool ValidateOrder(TableResponse table)
        {
            return table != null
                   && table.Data != null
                   && table.Data.All(item =>
                       !string.IsNullOrEmpty(item.Symbol)
                       && !string.IsNullOrEmpty(item.OrderID));
        }

        public void Start()
        {
            _socketSubscriber.Subscribe(BitmexTopic.order, HandleResponseAsync);
            _socketSubscriber.Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _socketSubscriber.Stop();
        }
    }
}
