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
    internal class BitMexPriceController
    {
        private readonly ILog _log;
        private readonly BitMexModelConverter _mapper;
        private readonly Func<TickPrice, Task> _tickPriceHandler;

        public BitMexPriceController(BitMexModelConverter mapper, Func<TickPrice, Task> tickPriceHandler, ILog log)
        {
            _mapper = mapper;
            _tickPriceHandler = tickPriceHandler;
            _log = log;
        }

        public async Task HandleResponseAsync(TableResponse table)
        {
            if (!ValidateQuote(table))
            {
                await _log.WriteWarningAsync(nameof(BitMexPriceController), nameof(HandleResponseAsync),
                    $"Ignoring invalid 'quote' message: '{JsonConvert.SerializeObject(table)}'");
                return;
            }

            if (table.Action == Action.Insert)
            {
                var prices = table.Data.Select(q => _mapper.QuoteToModel(q));
                foreach (var price in prices)
                {
                    await _tickPriceHandler(price);
                }
            }
        }

        private bool ValidateQuote(TableResponse table)
        {
            return table != null
                   && table.Data != null
                   && table.Data.All(item => item.AskPrice.HasValue && item.BidPrice.HasValue);
        }
    }
}
