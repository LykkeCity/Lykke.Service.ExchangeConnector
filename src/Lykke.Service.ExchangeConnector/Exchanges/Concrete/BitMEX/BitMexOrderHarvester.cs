using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Infrastructure.Configuration;
using TradingBot.Trading;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexOrderHarvester
    {
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private Func<Acknowledgement, Task> _ackHandler;
        private Func<ExecutedTrade, Task> _tradeHandler;

        public BitMexOrderHarvester(string exchangeName,
            BitMexExchangeConfiguration configuration,
            IBitmexSocketSubscriber socketSubscriber,
            ILog log)
        {
            _log = log;
            socketSubscriber.Subscribe(BitmexTopic.Order, HandleResponseAsync);
            _mapper = new BitMexModelConverter(configuration.SupportedCurrencySymbols, exchangeName);
        }

        public void AddAcknowledgementHandler(Func<Acknowledgement, Task> handler)
        {
            _ackHandler = handler;
        }

        public void AddExecutedTradeHandler(Func<ExecutedTrade, Task> handler)
        {
            _tradeHandler = handler;
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
                        await _ackHandler(ack);
                    }
                    break;
                case Action.Update:
                    foreach (var row in table.Data)
                    {
                        var trade = _mapper.OrderToTrade(row);
                        
                        if (trade.Status == ExecutionStatus.Unknown)
                        {
                            await _log.WriteWarningAsync(nameof(BitMexOrderHarvester), nameof(HandleResponseAsync),
                                $"Can't convert trade status {row.OrdStatus} into ExecutionStatus. Converted item: {trade}. Don't call handlers.");
                        }
                        else
                        {
                            await _tradeHandler(trade);   
                        }
                    }
                    break;
                case Action.Delete:
                default:
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
    }
}
