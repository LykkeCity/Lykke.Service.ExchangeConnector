using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Newtonsoft.Json;
using TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model;
using TradingBot.Trading;
using Action = TradingBot.Exchanges.Concrete.BitMEX.WebSocketClient.Model.Action;

namespace TradingBot.Exchanges.Concrete.BitMEX
{
    internal class BitMexOrdersController
    {
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private readonly Func<Acknowledgement, Task> _ackHandler;
        private readonly Func<ExecutedTrade, Task> _tradeHandler;

        public BitMexOrdersController(BitMexModelConverter mapper, 
            Func<Acknowledgement, Task> ackHandler,
            Func<ExecutedTrade, Task> tradeHandler,
            ILog log)
        {
            _mapper = mapper;
            _ackHandler = ackHandler;
            _tradeHandler = tradeHandler;
            _log = log;
        }

        public async Task HandleResponseAsync(TableResponse table)
        {
            if (!ValidateOrder(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexOrdersController), nameof(HandleResponseAsync),
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
                    var trades = table.Data.Select(row => _mapper.OrderToTrade(row));
                    foreach (var trade in trades)
                    {
                        await _tradeHandler(trade);
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
